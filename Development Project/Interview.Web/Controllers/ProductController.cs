using Interview.Web.Classes;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Interview.Web.Controllers
{
    // NOTE: main route for all api calls
    [Route("api/v1/products")]
    [ApiController]
    public class ProductController : Controller
    {
        // NOTE: Outside API Handler To Get Every Product localhost:port/api/v1/products/GetAllProducts
        [HttpGet]
        [Route("GetAllProducts")]
        public Task<IActionResult> GetAllProducts()
        {
            return Task.FromResult((IActionResult)Ok(GetProducts()));
        }

        // NOTE: Outside API Handler To Search Products, Categories, and metadata therein at localhost:port/api/v1/products/SearchForProducts/<InsertSearchTermHere>
        [HttpPost]
        [Route("AddNewProduct")]
        public Task<IActionResult> AddNewProduct(iProduct product)
        {
            return Task.FromResult((IActionResult)Ok(new object[] { AddProduct(product) }));
        }

        // NOTE: Outside API Handler To Search Products, Categories, and metadata therein at localhost:port/api/v1/products/SearchForProducts/<InsertSearchTermHere>
        [HttpGet]
        [Route("[action]/{descriptor:string}")]
        public Task<IActionResult> SearchForProducts(string descriptor)
        {
            List<iProduct> products = SearchProducts(descriptor);
            products.AddRange(SearchProductAttributes(descriptor));
            List<iCategory> categories = SearchCategories(descriptor);
            categories.AddRange(SearchCategoryAttributes(descriptor));

            iSearchResponse searchResponse = new iSearchResponse();
            searchResponse.products = products;
            searchResponse.categories = categories;

            return Task.FromResult((IActionResult)Ok(new object[] { searchResponse }));
        }

        // NOTE: Outside API Handler To Search Products, Categories, and metadata therein at localhost:port/api/v1/products/SearchForProducts/<InsertSearchTermHere>
        [HttpPost]
        [Route("AdjustInventory")]
        public Task<IActionResult> AdjustInventory(iInventoryRequest inventoryRequest)
        {
            return Task.FromResult((IActionResult)Ok(new object[] { InsertInventoryChange(inventoryRequest) }));
        }

        // NOTE: Outside API Handler To Search Products, Categories, and metadata therein at localhost:port/api/v1/products/SearchForProducts/<InsertSearchTermHere>
        [HttpGet]
        [Route("[action]/{InstanceId:int}")]
        public Task<IActionResult> GetInventoryCountForItem(int InstanceId)
        {
            return Task.FromResult((IActionResult)Ok(new object[] { GetInventoryCount(InstanceId) }));
        }

        /*
         * adds a new product to the database, returns false on error, true on success
         */
        protected bool AddProduct(iProduct product)
        {
            try
            {
                SqlConnectionStringBuilder sqlConnectionStringBuilder = GetSqlConnectionStringBuilder();

                using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionStringBuilder.ConnectionString))
                {
                    sqlConnection.Open();

                    string insertProductSQL = "INSERT INTO products (InstanceId, Name, Description, ProductImageUris, ValidSkus, CreatedTimestamp) values(@InstanceId, @Name, @Description, @ProductImageUris, @ValidSkus, @CreatedTimestamp)";

                    using (SqlCommand sqlCommand = new SqlCommand(insertProductSQL, sqlConnection))
                    {
                        sqlCommand.Parameters.AddWithValue("@InstanceId", product.InstanceId);
                        sqlCommand.Parameters.AddWithValue("@Name", product.Name);
                        sqlCommand.Parameters.AddWithValue("@Description", product.Description);
                        sqlCommand.Parameters.AddWithValue("@ProductImageUris", product.ProductImageUris);
                        sqlCommand.Parameters.AddWithValue("@ValidSkus", product.ValidSkus);
                        sqlCommand.Parameters.AddWithValue("@CreatedTimestamp", new System.DateTime());

                        sqlCommand.ExecuteNonQuery();
                    }
                }

                using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionStringBuilder.ConnectionString))
                {
                    sqlConnection.Open();

                    foreach (iAttribute attribute in product.Attributes)
                    {
                        string allProductSQL = "INSERT INTO ProductAttributes (InstanceId, Key, Value) values(@InstanceId, @Key, @Value)";

                        using (SqlCommand sqlCommand = new SqlCommand(allProductSQL, sqlConnection))
                        {
                            sqlCommand.Parameters.AddWithValue("@InstanceId", product.InstanceId);
                            sqlCommand.Parameters.AddWithValue("@Key", attribute.Key);
                            sqlCommand.Parameters.AddWithValue("@Value", attribute.Value);

                            sqlCommand.ExecuteNonQuery();
                        }
                    }
                }

                using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionStringBuilder.ConnectionString))
                {
                    sqlConnection.Open();

                    foreach (iInstanceCategory category in product.Categories)
                    {
                        string allProductSQL = "INSERT INTO ProductCategories (InstanceId, CategoryInstanceId) values(@InstanceId, @CategoryInstanceId)";

                        using (SqlCommand sqlCommand = new SqlCommand(allProductSQL, sqlConnection))
                        {
                            sqlCommand.Parameters.AddWithValue("@InstanceId", category.InstanceId);
                            sqlCommand.Parameters.AddWithValue("@CategoryInstanceID", category.CategoryInstanceId);

                            sqlCommand.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }

            return true;
        }

        /*
         * Returns a list of all products in the database
         */
        protected List<iProduct> GetProducts()
        {
            List<iProduct> products = new List<iProduct>();

            try
            {
                SqlConnectionStringBuilder sqlConnectionStringBuilder = GetSqlConnectionStringBuilder();

                using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionStringBuilder.ConnectionString))
                {
                    sqlConnection.Open();

                    string allProductSQL = "SELECT * FROM Products";

                    using (SqlCommand sqlCommand = new SqlCommand(allProductSQL, sqlConnection))
                    {
                        using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                        {
                            while (sqlDataReader.Read())
                            {
                                iProduct tmpVal = new iProduct();

                                tmpVal.InstanceId = sqlDataReader.GetInt32(0);
                                tmpVal.Name = sqlDataReader.GetString(1);
                                tmpVal.Description = sqlDataReader.GetString(2);
                                tmpVal.ProductImageUris = sqlDataReader.GetString(3);
                                tmpVal.ValidSkus = sqlDataReader.GetString(4);
                                tmpVal.CreatedTimestamp = sqlDataReader.GetDateTime(5);

                                tmpVal.Attributes.AddRange(GetAttributesForType(tmpVal.InstanceId, "PRODUCT"));
                                tmpVal.Categories.AddRange(GetCategoriesForCategory(tmpVal.InstanceId));

                                products.Add(tmpVal);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return products;
        }

        /*
         * returns a list of products from a provided descriptor
         */
        protected List<iProduct> SearchProducts(string descriptor)
        {
            List<iProduct> products = new List<iProduct>();

            try
            {
                SqlConnectionStringBuilder sqlConnectionStringBuilder = GetSqlConnectionStringBuilder();

                using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionStringBuilder.ConnectionString))
                {
                    string searchProductSQL = "SELECT * FROM Products p WHERE ((UPPER(p.Name) LIKE UPPER(@term)) OR (UPPER(p.Description) LIKE UPPER(@term)))";

                    sqlConnection.Open();

                    using (SqlCommand sqlCommand = new SqlCommand(searchProductSQL, sqlConnection))
                    {
                        sqlCommand.Parameters.Add("term", System.Data.SqlDbType.VarChar).Value = descriptor;

                        using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                        {
                            while (sqlDataReader.Read())
                            {
                                iProduct tmpVal = new iProduct();
                                tmpVal.InstanceId = sqlDataReader.GetInt32(0);
                                tmpVal.Name = sqlDataReader.GetString(1);
                                tmpVal.Description = sqlDataReader.GetString(2);
                                tmpVal.ProductImageUris = sqlDataReader.GetString(3);
                                tmpVal.ValidSkus = sqlDataReader.GetString(4);
                                tmpVal.CreatedTimestamp = sqlDataReader.GetDateTime(5);

                                tmpVal.Attributes.AddRange(GetAttributesForType(tmpVal.InstanceId, "PRODUCT"));
                                tmpVal.Categories.AddRange(GetCategoriesForCategory(tmpVal.InstanceId));

                                products.Add(tmpVal);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return products;
        }

        /*
         * returns a list of products whose attributes' values match the parameter's value
         */
        protected List<iProduct> SearchProductAttributes(string descriptor)
        {
            List<iProduct> productList = new List<iProduct>();

            try
            {
                SqlConnectionStringBuilder sqlConnectionStringBuilder = GetSqlConnectionStringBuilder();

                using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionStringBuilder.ConnectionString))
                {
                    string productAttributeSQL = "SELECT * FROM Products p INNER JOIN ProductAttributes pa on pa.InstanceId = p.InstanceId WHERE ((UPPER(pa.Value) LIKE UPPER(@term)))";

                    sqlConnection.Open();

                    using (SqlCommand sqlCommand = new SqlCommand(productAttributeSQL, sqlConnection))
                    {
                        sqlCommand.Parameters.Add("term", System.Data.SqlDbType.VarChar).Value = descriptor;

                        using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                        {
                            while (sqlDataReader.Read())
                            {
                                iProduct tmpVal = new iProduct();
                                tmpVal.InstanceId = sqlDataReader.GetInt32(0);
                                tmpVal.Name = sqlDataReader.GetString(1);
                                tmpVal.Description = sqlDataReader.GetString(2);
                                tmpVal.ProductImageUris = sqlDataReader.GetString(3);
                                tmpVal.ValidSkus = sqlDataReader.GetString(4);
                                tmpVal.CreatedTimestamp = sqlDataReader.GetDateTime(5);

                                tmpVal.Attributes.AddRange(GetAttributesForType(tmpVal.InstanceId, "PRODUCT"));
                                tmpVal.Categories.AddRange(GetCategoriesForCategory(tmpVal.InstanceId));

                                productList.Add(tmpVal);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return productList;
        }

        /*
         * returns a list of categories if it matches the search term in name or description
         */
        protected List<iCategory> SearchCategories(string descriptor)
        {
            List<iCategory> categoryList = new List<iCategory>();

            try
            {
                SqlConnectionStringBuilder sqlConnectionStringBuilder = GetSqlConnectionStringBuilder();

                using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionStringBuilder.ConnectionString))
                {
                    string productAttributeSQL = "SELECT * FROM Categories c WHERE ((UPPER(c.Name) LIKE UPPER(@term)) OR (UPPER(c.Description) LIKE UPPER(@term)))";

                    sqlConnection.Open();

                    using (SqlCommand sqlCommand = new SqlCommand(productAttributeSQL, sqlConnection))
                    {
                        sqlCommand.Parameters.Add("term", System.Data.SqlDbType.VarChar).Value = descriptor;

                        using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                        {
                            while (sqlDataReader.Read())
                            {
                                iCategory category = new iCategory();
                                category.InstanceID = sqlDataReader.GetInt32(0);
                                category.Name = sqlDataReader.GetString(1);
                                category.Description = sqlDataReader.GetString(2);
                                category.CreatedTimeStamp = sqlDataReader.GetDateTime(3);

                                category.Attributes.AddRange(GetAttributesForType(category.InstanceID, "CATEGORY"));

                                foreach (iInstanceCategory i in GetCategoriesForCategory(category.InstanceID))
                                {
                                    category.Categories.Add(GetCategoryById(i.CategoryInstanceId));
                                }

                                categoryList.Add(category);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return categoryList;
        }

        /*
         * returns a list of categories whose attributes' values match the parameter's value
         */
        protected List<iCategory> SearchCategoryAttributes(string descriptor)
        {
            List<iCategory> categoryList = new List<iCategory>();

            try
            {
                SqlConnectionStringBuilder sqlConnectionStringBuilder = GetSqlConnectionStringBuilder();

                using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionStringBuilder.ConnectionString))
                {
                    string categoryAttributeSQL = "SELECT * FROM Categories c INNER JOIN CategoryAttributes ca on ca.InstanceId = c.InstanceId WHERE ((UPPER(ca.Value) LIKE UPPER(@term)))";

                    sqlConnection.Open();

                    using (SqlCommand sqlCommand = new SqlCommand(categoryAttributeSQL, sqlConnection))
                    {
                        sqlCommand.Parameters.Add("term", System.Data.SqlDbType.VarChar).Value = descriptor;

                        using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                        {
                            while (sqlDataReader.Read())
                            {
                                iCategory tmpCategory = new iCategory();
                                tmpCategory.InstanceID = sqlDataReader.GetInt32(0);
                                tmpCategory.Name = sqlDataReader.GetString(1);
                                tmpCategory.Description = sqlDataReader.GetString(2);
                                tmpCategory.CreatedTimeStamp = sqlDataReader.GetDateTime(3);

                                tmpCategory.Attributes.AddRange(GetAttributesForType(tmpCategory.InstanceID, "CATEGORY"));

                                foreach (iInstanceCategory i in GetCategoriesForCategory(tmpCategory.InstanceID))
                                {
                                    tmpCategory.Categories.Add(GetCategoryById(i.CategoryInstanceId));
                                }

                                categoryList.Add(tmpCategory);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return categoryList;
        }

        /*
         * returns a list of categories for a category
         */
        protected List<iInstanceCategory> GetCategoriesForCategory(int CategoryId)
        {
            List<iInstanceCategory> instanceCategories = new List<iInstanceCategory>();

            try
            {
                SqlConnectionStringBuilder sqlConnectionStringBuilder = GetSqlConnectionStringBuilder();

                using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionStringBuilder.ConnectionString))
                {
                    string categoryAttributeSQL = "SELECT * FROM CategoryCategories cc WHERE cc.CategoryInstanceId = @term";

                    sqlConnection.Open();

                    using (SqlCommand sqlCommand = new SqlCommand(categoryAttributeSQL, sqlConnection))
                    {
                        sqlCommand.Parameters.Add("term", System.Data.SqlDbType.Int).Value = CategoryId;

                        using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                        {
                            while (sqlDataReader.Read())
                            {
                                iInstanceCategory tmpInstanceCategory = new iInstanceCategory();
                                tmpInstanceCategory.InstanceId = sqlDataReader.GetInt32(0);
                                tmpInstanceCategory.CategoryInstanceId = sqlDataReader.GetInt32(1);

                                instanceCategories.Add(tmpInstanceCategory);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return instanceCategories;
        }

        protected iCategory GetCategoryById(int CategoryId)
        {
            iCategory category = new iCategory();

            try
            {
                SqlConnectionStringBuilder sqlConnectionStringBuilder = GetSqlConnectionStringBuilder();

                using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionStringBuilder.ConnectionString))
                {
                    string categoryAttributeSQL = "SELECT * FROM Categories c WHERE c.InstanceId = @CategoryId";

                    sqlConnection.Open();

                    using (SqlCommand sqlCommand = new SqlCommand(categoryAttributeSQL, sqlConnection))
                    {
                        sqlCommand.Parameters.Add("CategoryId", System.Data.SqlDbType.Int).Value = CategoryId;

                        using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                        {
                            while (sqlDataReader.Read())
                            {
                                category = new iCategory();
                                category.InstanceID = sqlDataReader.GetInt32(0);
                                category.Name = sqlDataReader.GetString(1);
                                category.Description = sqlDataReader.GetString(2);
                                category.CreatedTimeStamp = sqlDataReader.GetDateTime(3);

                                category.Attributes.AddRange(GetAttributesForType(category.InstanceID, "CATEGORY"));

                                foreach (iInstanceCategory i in GetCategoriesForCategory(category.InstanceID))
                                {
                                    category.Categories.Add(GetCategoryById(i.CategoryInstanceId));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return category;
        }

        /*
         * returns a list of attributes for an instance id, based on type (product or category)
         */
        protected List<iAttribute> GetAttributesForType(int InstanceId, string Type)
        {
            List<iAttribute> attributes = new List<iAttribute>();

            try
            {
                SqlConnectionStringBuilder sqlConnectionStringBuilder = GetSqlConnectionStringBuilder();

                using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionStringBuilder.ConnectionString))
                {
                    string attributeSQL = String.Format("SELECT * FROM {0} a WHERE ((UPPER(a.Value) LIKE UPPER (@term)))", Type.ToUpper() == "PRODUCT" ? "ProductAttributes" : "CategoryAttributes");

                    using (SqlCommand command = new SqlCommand(attributeSQL, sqlConnection))
                    {
                        command.Parameters.Add("searchTerm", System.Data.SqlDbType.Int).Value = InstanceId;

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                iAttribute tmpAttribute = new iAttribute();
                                tmpAttribute.InstanceId = reader.GetInt32(0);
                                tmpAttribute.Key = reader.GetString(1);
                                tmpAttribute.Value = reader.GetString(2);

                                attributes.Add(tmpAttribute);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }

            return attributes;
        }

        /*
         * adds a new inventory adjustment to the database, returns false on error, true on success
         */
        protected bool InsertInventoryChange(iInventoryRequest inventoryRequest)
        {
            try
            {
                SqlConnectionStringBuilder sqlConnectionStringBuilder = GetSqlConnectionStringBuilder();

                using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionStringBuilder.ConnectionString))
                {
                    sqlConnection.Open();

                    string insertProductSQL = "INSERT INTO ProductAttributes (InstanceId, Key, Value) Values (@InstanceId, @Key, @Value)";

                    using (SqlCommand sqlCommand = new SqlCommand(insertProductSQL, sqlConnection))
                    {
                        sqlCommand.Parameters.AddWithValue("@InstanceId", inventoryRequest.InstanceId);
                        sqlCommand.Parameters.AddWithValue("@Key", "INV");
                        sqlCommand.Parameters.AddWithValue("@Value", inventoryRequest.Amount.ToString());

                        sqlCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }

            return true;
        }

        protected double GetInventoryCount(int InstanceId)
        {
            double value = 0D;

            try
            {
                SqlConnectionStringBuilder sqlConnectionStringBuilder = GetSqlConnectionStringBuilder();

                using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionStringBuilder.ConnectionString))
                {
                    sqlConnection.Open();

                    string getInventorySql = @"Select p.InstanceId, CAST(pa.Value AS DECIMAL(10, 4)) as Inventory
                                               FROM Products p
                                               LEFT JOIN ProductAttributes pa on p.InstanceId = pa.InstanceId
                                               WHERE p.InstanceId = @InstanceId
                                               GROUP BY p.InstanceId";

                    using (SqlCommand sqlCommand = new SqlCommand(getInventorySql, sqlConnection))
                    {
                        value = (double)sqlCommand.ExecuteScalar();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return -86D;
            }

            return value;
        }

        protected SqlConnectionStringBuilder GetSqlConnectionStringBuilder()
        {
            SqlConnectionStringBuilder sqlConnectionStringBuilder = new SqlConnectionStringBuilder();

            sqlConnectionStringBuilder.DataSource = @"(localdb)\ProjectModels";
            sqlConnectionStringBuilder.InitialCatalog = "Sparcpoint.Inventory.Database";
            sqlConnectionStringBuilder.IntegratedSecurity = true;
            sqlConnectionStringBuilder.Pooling = false;
            sqlConnectionStringBuilder.ConnectTimeout = 30;

            return sqlConnectionStringBuilder;
        }
    }
}
