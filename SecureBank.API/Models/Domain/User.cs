using System.ComponentModel.DataAnnotations;

namespace SecureBank.API.Models.Domain
{
    public class User
    {
        [Key]
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Username {  get; set; }
        public string Password { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Role { get; set; }

        // Navigation property - One user can have many accounts
        public virtual ICollection<Account> Accounts { get; set; }
        public User()
        {
            Accounts = new List<Account>();
            CreatedDate = DateTime.UtcNow;
        }
    }
}
