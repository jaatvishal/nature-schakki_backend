using Core.Entities;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class ProductRepository(StoreContext context) : IProductRespository
{
    // here i create a primary constructor to inject the StoreContext
    public async Task<IReadOnlyList<Product>> GetProductsAsync(string? brand,string? type,string? sort)
    {
        try
        {

       
        var query = context.Products.AsQueryable();

        if(!string.IsNullOrWhiteSpace(brand) )
            query=query.Where(x => x.Brand == brand);   
        if(!string.IsNullOrWhiteSpace(type) )
            query=query.Where(x=>x.Type == type);
        
            query = sort switch
            {
                "priceAsc" => query.OrderBy(x => x.Price),
                "priceDesc" => query.OrderByDescending(x => x.Price),
                _ => query.OrderBy(x => x.Name)
            };


        return await query.ToListAsync();
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public void AddProduct(Product product)
    {try
        {
             context.Products.Add(product);
        }
        catch(Exception ex)
        {
            
        }
      
    }

    public void DeleteProduct(Product product)
    {
        try
        {
             context.Products.Remove(product);
        }
        catch(Exception ex)
        {
            
        }
        
    }

    public async Task<IReadOnlyList<string>> GetBrandsAsync()
    {
        try
        {
             return await context.Products.Select(p => p.Brand).Distinct().ToListAsync();
        }
        catch(Exception ex)
        {
            return null;
        }
       
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        try
        {
             return await context.Products.FindAsync(id);
        }
        catch(Exception ex)
        {
            return null;
        }
       
    }

    public async Task<IReadOnlyList<Product>> GetProductsAsync()
    {
        try
        {
             return await context.Products.ToListAsync();
        } 
        catch(Exception ex)
        {
            return null;
        }
      
    }

    public async Task<IReadOnlyList<string>> GetTypesAsync()
    {
       try
        {
             return await context.Products.Select(p => p.Type).Distinct().ToListAsync();
        }
        catch(Exception ex)
        {
            return null;
        }
    }

    public bool ProductExists(int id)
    {
        try
        {
             return context.Products.Any(e => e.Id == id);
        }
        catch (Exception ex)
        {
            return false;
        }
       
    }

    public async Task<bool> SaveChangesAsync()
    {
        try
        {
             return await context.SaveChangesAsync() > 0;
        }
        catch(Exception ex)
        {
            return false;
        }
        
    }

    public void UpdateProduct(Product product)
    {
        try
        {
             context.Entry(product).State = EntityState.Modified;
        }
        catch(Exception ex)
        {
            
        }
       
    }
}


