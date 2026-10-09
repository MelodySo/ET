using MongoDB.Bson.Serialization.Attributes;

namespace ET.Server
{
    [EnableClass]
    public sealed class LoginAccount
    {
        [BsonId]
        public long Id { get; set; }
        public string Account { get; set; }
        public string PasswordSalt { get; set; }
        public string PasswordHash { get; set; }
        public int PasswordIterations { get; set; }
        public long CreatedAt { get; set; }
    }
}
