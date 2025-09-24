using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecureBank.API.Models.Domain;
using SecureBank.API.Models.DTO;
using SecureBank.API.Repositories.Interface;

namespace SecureBank.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactsController : ControllerBase
    {
        private readonly IContactRepository _contactRepository;

        public ContactsController(IContactRepository contactRepository)
        {
            _contactRepository = contactRepository;
        }

        // GET: api/Contacts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ContactDto>>> GetContacts()
        {
            var contacts = await _contactRepository.GetContactsAsync();
            var dtos = contacts.Select(c => new ContactDto
            {
                ContactId = c.ContactId,
                Name = c.Name,
                Surname = c.Surname,
                PhoneNumber = c.PhoneNumber,
                Subject = c.Subject,
                Message = c.Message
            }).ToList();

            return Ok(dtos);
        }

        // GET: api/Contacts/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ContactDto>> GetContact(int id)
        {
            var contact = await _contactRepository.GetByIdAsync(id);
            if (contact == null)
                return NotFound();

            var dto = new ContactDto
            {
                ContactId = contact.ContactId,
                Name = contact.Name,
                Surname = contact.Surname,
                PhoneNumber = contact.PhoneNumber,
                Subject = contact.Subject,
                Message = contact.Message
            };
            return Ok(dto);
        }

        // POST: api/Contacts
        [HttpPost]
        public async Task<ActionResult<ContactDto>> AddContact([FromBody] AddContactRequestDto request)
        {
            var contact = new Contact
            {
                Name = request.Name,
                Surname = request.Surname,
                PhoneNumber = request.PhoneNumber,
                Subject = request.Subject,
                Message = request.Message
            };

            var created = await _contactRepository.CreateAsync(contact);

            var dto = new ContactDto
            {
                ContactId = created.ContactId,
                Name = created.Name,
                Surname = created.Surname,
                PhoneNumber = created.PhoneNumber,
                Subject = created.Subject,
                Message = created.Message
            };

            return CreatedAtAction(nameof(GetContact), new { id = created.ContactId }, dto);
        }

        // PUT: api/Contacts/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<ContactDto>> UpdateContact(int id, [FromBody] AddContactRequestDto request)
        {
            var contact = new Contact
            {
                Name = request.Name,
                Surname = request.Surname,
                PhoneNumber = request.PhoneNumber,
                Subject = request.Subject,
                Message = request.Message
            };

            var updated = await _contactRepository.UpdateAsync(id, contact);

            if (updated == null)
                return NotFound();

            var dto = new ContactDto
            {
                ContactId = updated.ContactId,
                Name = updated.Name,
                Surname = updated.Surname,
                PhoneNumber = updated.PhoneNumber,
                Subject = updated.Subject,
                Message = updated.Message
            };

            return Ok(dto);
        }

        // DELETE: api/Contacts/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteContact(int id)
        {
            var deleted = await _contactRepository.DeleteAsync(id);
            if (deleted == null)
                return NotFound();
            return NoContent();
        }
    }
}
