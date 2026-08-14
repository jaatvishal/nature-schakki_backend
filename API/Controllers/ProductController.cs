using API.RequestHelpers;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
namespace API.Controllers.ProductController;

//[ApiController]
//[Route("api/[controller]")]
public class ProductController(IGenericRepository<Product> repo) : BaseApiController
{

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Product>>> GetProducts([FromQuery]ProductSpecParams specParams)
    {
        try
        {
            var spec = new ProductSpecification(specParams);

            return await CreatePagedResult(repo, spec, specParams.PageIndex, specParams.PageSize);

           
        }
        catch (Exception ex)
        {
            return null;
        }

    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        try
        {
            var product = await repo.GetByIdAsync(id);
            if (product == null) return NotFound();

            return product;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(Product product)
    {
        try
        {
            repo.Add(product);
            if (await repo.SaveAllAsync())
            {
                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
            }
            return BadRequest("Failed to create product");
            // return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }
        catch (Exception ex)
        {
            return null;
        }

    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, Product product)
    {
        try
        {
            if (id != product.Id || !ProductExists(id)) return BadRequest("Cannot update this product");

            repo.Update(product);

            if (await repo.SaveAllAsync())
            {
                return NoContent();
            }

            return BadRequest("Failed to update product");
        }
        catch (Exception ex)
        {
            return null;
        }

    }
    private bool ProductExists(int id)
    {
        try
        {
            return repo.Exits(id);
        }
        catch (Exception ex)
        {
            return false;
        }

    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        try
        {
            var product = await repo.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            repo.Remove(product);
            if (await repo.SaveAllAsync())
            {
                return NoContent();
            }

            return BadRequest("problem to delete product");
        }
        catch (Exception ex)
        {
            return null;
        }

    }

    [HttpGet("brands")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetBrands()
    {
        try
        {
            var spec = new BrandListSpecification();

            return Ok(await repo.ListAsync(spec));
        }
        catch (Exception ex)
        {
            return null;
        }

    }
    [HttpGet("types")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetTypes()
    {
        try
        {
            var spec = new TypeListSpecification();

            return Ok(await repo.ListAsync(spec));
        }
        catch (Exception ex)
        {
            return null;
        }

    }
}