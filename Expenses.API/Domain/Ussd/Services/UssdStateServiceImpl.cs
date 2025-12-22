using Expenses.API.Domain.Ussd.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace Expenses.API.Domain.Ussd.Services {
    public class UssdStateServiceImpl(IConnectionMultiplexer redis) : UssdStateService {
        private readonly IDatabase _database = redis.GetDatabase();
        private const int SessionExpirationMinutes = 8; // 5-10 minute range, using 8 minutes
        private const string KeyPrefix = "ussd:state:";
        
        // Type discriminator for polymorphic deserialization
        private const string TypeDiscriminatorProperty = "$stateType";
        
        // Registry of known state types
        private static readonly Dictionary<string, Type> StateTypeRegistry = new() {
            { "UssdState", typeof(UssdState) },
            { "AddTransactionState", typeof(AddTransactionState) },
            { "TransactionHistoryState", typeof(TransactionHistoryState) }
        };

        public override async Task<UssdState?> GetStateAsync(string phoneNumber) {
            var key = GetKey(phoneNumber);
            var value = await _database.StringGetAsync(key);
            
            if (!value.HasValue) {
                return null;
            }

            try {
                // Explicitly convert to string to resolve ambiguity
                string jsonString = value.ToString();
                
                // Check for type discriminator
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;
                
                Type targetType = typeof(UssdState); // Default type
                
                if (root.TryGetProperty(TypeDiscriminatorProperty, out var typeElement)) {
                    var typeName = typeElement.GetString();
                    if (!string.IsNullOrEmpty(typeName) && StateTypeRegistry.TryGetValue(typeName, out var registeredType)) {
                        targetType = registeredType;
                    }
                }
                
                // Deserialize to the appropriate type
                return JsonSerializer.Deserialize(jsonString, targetType) as UssdState;
            } catch {
                return null;
            }
        }

        public override async Task SaveStateAsync(string phoneNumber, UssdState state) {
            var key = GetKey(phoneNumber);
            state.LastUpdated = DateTime.UtcNow;
            
            // Serialize with type discriminator
            var stateType = state.GetType();
            var typeName = stateType.Name; // Use simple type name
            
            // Serialize to JsonDocument first to add type discriminator
            var json = JsonSerializer.Serialize(state, stateType);
            using var doc = JsonDocument.Parse(json);
            
            // Create new JSON with type discriminator
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream)) {
                writer.WriteStartObject();
                writer.WriteString(TypeDiscriminatorProperty, typeName);
                
                // Copy all properties from original JSON
                foreach (var property in doc.RootElement.EnumerateObject()) {
                    property.WriteTo(writer);
                }
                
                writer.WriteEndObject();
            }
            
            var jsonWithType = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            
            await _database.StringSetAsync(
                key, 
                jsonWithType, 
                TimeSpan.FromMinutes(SessionExpirationMinutes)
            );
        }

        public override async Task ClearStateAsync(string phoneNumber) {
            var key = GetKey(phoneNumber);
            await _database.KeyDeleteAsync(key);
        }

        private static string GetKey(string phoneNumber) {
            return $"{KeyPrefix}{phoneNumber}";
        }
    }
}
