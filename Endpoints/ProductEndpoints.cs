using Microsoft.AspNetCore.Mvc;
using StoreManagementAPI.DTOs;
using StoreManagementAPI.Services;

namespace BlazorApp1.Endpoints
{
    /// <summary>
    /// Product API Endpoints using Minimal API
    /// </summary>
    public static class ProductEndpoints
    {
        public static void MapProductEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/products")
                .WithTags("Products");

            // GET: /api/products - Get all products
            group.MapGet("/", GetAllProducts)
                .WithName("GetAllProducts")
                .WithSummary("Get all products")
                .WithDescription("Retrieves all products with their category, supplier, and inventory information")
                .Produces<IEnumerable<ProductDto>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status500InternalServerError);

            // POST: /api/products/search - Advanced search with POST
            group.MapPost("/search", SearchProductsPost)
                .WithName("SearchProductsPost")
                .WithSummary("Search products with advanced criteria (POST)")
                .WithDescription("Search products by name, category, status, price range with pagination and sorting")
                .Produces<ProductSearchResultDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status500InternalServerError);

            // GET: /api/products/search - Advanced search with GET (query parameters)
            group.MapGet("/search", SearchProductsGet)
                .WithName("SearchProductsGet")
                .WithSummary("Search products with advanced criteria (GET)")
                .WithDescription("Search products using query parameters. Example: ?name=laptop&categoryId=1&status=active&minPrice=100&maxPrice=1000")
                .Produces<ProductSearchResultDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status500InternalServerError);

            // GET: /api/products/{id} - Get product by ID
            group.MapGet("/{id:int}", GetProductById)
                .WithName("GetProductById")
                .WithSummary("Get product by ID")
                .WithDescription("Retrieves a specific product by its ID")
                .Produces<ProductDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);

            // POST: /api/products - Create new product
            group.MapPost("/", CreateProduct)
                .WithName("CreateProduct")
                .WithSummary("Create a new product")
                .WithDescription("Creates a new product with the provided information")
                .Produces<ProductDto>(StatusCodes.Status201Created)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status500InternalServerError);

            // PUT: /api/products/{id} - Update product
            group.MapPut("/{id:int}", UpdateProduct)
                .WithName("UpdateProduct")
                .WithSummary("Update an existing product")
                .WithDescription("Updates product information by ID")
                .Produces<ProductDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);

            // DELETE: /api/products/{id} - Delete product
            group.MapDelete("/{id:int}", DeleteProduct)
                .WithName("DeleteProduct")
                .WithSummary("Delete a product")
                .WithDescription("Deletes a product by ID (soft delete if product has orders)")
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);
        }

        // Handler methods
        private static async Task<IResult> GetAllProducts(
            IProductService productService,
            ILogger<Program> logger)
        {
            try
            {
                var products = await productService.GetAllProductsAsync();
                return Results.Ok(products);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting all products");
                return Results.Problem(
                    detail: "An error occurred while retrieving products",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        private static async Task<IResult> SearchProductsPost(
            [FromBody] ProductSearchDto searchDto,
            IProductService productService,
            ILogger<Program> logger)
        {
            try
            {
                // Validate input
                if (searchDto == null)
                {
                    return Results.BadRequest(new { message = "Search criteria cannot be null" });
                }

                // Validate price range
                if (searchDto.MinPrice.HasValue && searchDto.MaxPrice.HasValue)
                {
                    if (searchDto.MinPrice > searchDto.MaxPrice)
                    {
                        return Results.BadRequest(new { message = "MinPrice cannot be greater than MaxPrice" });
                    }
                }

                var result = await productService.SearchProductsAdvancedAsync(searchDto);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while searching products with criteria: {@SearchDto}", searchDto);
                return Results.Problem(
                    detail: "An error occurred while searching products",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        private static async Task<IResult> SearchProductsGet(
            [AsParameters] ProductSearchQueryParams queryParams,
            IProductService productService,
            ILogger<Program> logger)
        {
            try
            {
                // Validate price range
                if (queryParams.MinPrice.HasValue && queryParams.MaxPrice.HasValue
                    && queryParams.MinPrice > queryParams.MaxPrice)
                {
                    return Results.BadRequest(new { message = "MinPrice cannot be greater than MaxPrice" });
                }

                var searchDto = new ProductSearchDto
                {
                    Name = queryParams.Name,
                    CategoryId = queryParams.CategoryId,
                    Status = queryParams.Status,
                    MinPrice = queryParams.MinPrice,
                    MaxPrice = queryParams.MaxPrice,
                    PageNumber = queryParams.PageNumber,
                    PageSize = queryParams.PageSize,
                    SortBy = queryParams.SortBy,
                    SortDirection = queryParams.SortDirection
                };

                var result = await productService.SearchProductsAdvancedAsync(searchDto);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while searching products");
                return Results.Problem(
                    detail: "An error occurred while searching products",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        private static async Task<IResult> GetProductById(
            int id,
            IProductService productService,
            ILogger<Program> logger)
        {
            try
            {
                var product = await productService.GetProductByIdAsync(id);

                if (product == null)
                {
                    return Results.NotFound(new { message = $"Product with ID {id} not found" });
                }

                return Results.Ok(product);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting product {ProductId}", id);
                return Results.Problem(
                    detail: "An error occurred while retrieving the product",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        private static async Task<IResult> CreateProduct(
            [FromBody] CreateProductDto createDto,
            IProductService productService,
            ILogger<Program> logger)
        {
            try
            {
                if (createDto == null)
                {
                    return Results.BadRequest(new { message = "Product data cannot be null" });
                }

                var product = await productService.CreateProductAsync(createDto);
                return Results.Created($"/api/products/{product.ProductId}", product);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while creating product");
                return Results.Problem(
                    detail: "An error occurred while creating the product",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        private static async Task<IResult> UpdateProduct(
            int id,
            [FromBody] UpdateProductDto updateDto,
            IProductService productService,
            ILogger<Program> logger)
        {
            try
            {
                if (updateDto == null)
                {
                    return Results.BadRequest(new { message = "Product data cannot be null" });
                }

                var product = await productService.UpdateProductAsync(id, updateDto);

                if (product == null)
                {
                    return Results.NotFound(new { message = $"Product with ID {id} not found" });
                }

                return Results.Ok(product);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while updating product {ProductId}", id);
                return Results.Problem(
                    detail: "An error occurred while updating the product",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        private static async Task<IResult> DeleteProduct(
            int id,
            IProductService productService,
            ILogger<Program> logger)
        {
            try
            {
                var result = await productService.DeleteProductAsync(id);

                if (!result.Success)
                {
                    return Results.NotFound(new { message = result.Message });
                }

                return Results.Ok(new { message = result.Message, softDeleted = result.SoftDeleted });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while deleting product {ProductId}", id);
                return Results.Problem(
                    detail: "An error occurred while deleting the product",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }

    /// <summary>
    /// Query parameters for product search (GET method)
    /// </summary>
    public record ProductSearchQueryParams(
        string? Name,
        int? CategoryId,
        string? Status,
        decimal? MinPrice,
        decimal? MaxPrice,
        int PageNumber = 1,
        int PageSize = 20,
        string? SortBy = "ProductName",
        string? SortDirection = "asc"
    );
}
