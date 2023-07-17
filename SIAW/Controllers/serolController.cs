using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;

namespace SIAW.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class serolController : ControllerBase
    {
        /* 
        private readonly PSContext _context;
        
        public serolController(PSContext context)
        {
            _context = context;
        }
        */

        private readonly DBContext _context;

        public serolController()
        {
            string connectionString = ConnectionController.ConnectionString;
            _context = DbContextFactory.Create(connectionString);
        }

        // GET: api/serol
        [HttpGet]
        public async Task<ActionResult<IEnumerable<serol>>> Getserol()
        {
            if (_context.serol == null)
            {
                return NotFound();
            }
            return await _context.serol.ToListAsync();

        }

        // GET: api/serol/5
        [HttpGet("{id}")]
        public async Task<ActionResult<serol>> Getserol(string id)
        {
            if (_context.serol == null)
            {
                return NotFound();
            }
            var serol = await _context.serol.FindAsync(id);

            if (serol == null)
            {
                return NotFound();
            }

            return serol;

        }

        // PUT: api/serol/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> Putserol(string id, serol serol)
        {
            if (id != serol.codigo)
            {
                return BadRequest();
            }

            _context.Entry(serol).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!serolExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();

        }

        // POST: api/serol
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<serol>> Postserol(serol serol)
        {
            if (_context.serol == null)
            {
                return Problem("Entity set 'PSContext.serol'  is null.");
            }
            _context.serol.Add(serol);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (serolExists(serol.codigo))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("Getserol", new { id = serol.codigo }, serol);

        }

        // DELETE: api/serol/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Deleteserol(string id)
        {
            if (_context.serol == null)
            {
                return NotFound();
            }
            var serol = await _context.serol.FindAsync(id);
            if (serol == null)
            {
                return NotFound();
            }

            _context.serol.Remove(serol);
            await _context.SaveChangesAsync();

            return NoContent();

        }

        private bool serolExists(string id)
        {
            return (_context.serol?.Any(e => e.codigo == id)).GetValueOrDefault();

        }
    }
}
