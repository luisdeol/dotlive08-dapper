// See https://aka.ms/new-console-template for more information
using System.Data;
using System.Data.SqlClient;
using Dapper;

var connectionString = "Server=localhost; Database=DotLive08_Ecommerce; User Id=sa; Password=MyPass@word;trustServerCertificate=true;MultipleActiveResultSets=True";


using (var connection = new SqlConnection(connectionString))
{
    connection.Open();

    // INSERT e UPDATE
    var sqlInsert = "INSERT INTO Orders OUTPUT INSERTED.OrderId VALUES (@CustomerId, @OrderDetails)";

    var id = connection.ExecuteScalar<int>(sqlInsert, new { CustomerId = 1, OrderDetails = "Detalhes" });

    var sqlUpdate = "UPDATE Orders SET OrderDetails = @OrderDetails WHERE OrderId = @OrderId";

    connection.Execute(sqlUpdate, new { OrderDetails = "Updated Details", OrderId = id });

    // Stored Procedure
    var parameters = new DynamicParameters();
    parameters.Add("@OrderId", 2);

    var orderDetailsFromSp = connection.Query<OrderDetailResult>("GetOrderDetails",
        parameters, commandType: CommandType.StoredProcedure).SingleOrDefault();

    // SELECT simples (com ou sem projeção)
    var sqlOrderById = @"SELECT * FROM Orders WHERE OrderId = @OrderId";

    var orderByIdResult = connection.QuerySingleOrDefault<Order>(sqlOrderById,
        new { OrderId = 2 });

    var sqlAllOrders = @"SELECT * FROM Orders";

    var allOrdersResult = connection.Query<Order>(sqlAllOrders).ToList();

    var sqlOrderByIdProjection = @"SELECT 
        o.OrderId, o.OrderDetails, 
        c.CustomerId, c.CustomerName 
        FROM Orders o
        INNER JOIN Customers c ON o.CustomerId = c.CustomerId
        WHERE o.OrderId = @OrderId;";

    var orderByIdProjectResult = connection.Query<OrderDetailResult>(sqlOrderByIdProjection,
        new { OrderId = 2 }).SingleOrDefault();

    // SELECT com Objetos Mesclados
    var orderWithCustomerResult = connection.Query<Order, Customer, Order>(sqlOrderByIdProjection,
            (order, customer) =>
            {
                order.Customer = customer;
                order.CustomerId = customer.CustomerId;

                return order;
            },
            param: parameters,
            splitOn: "CustomerId")
        .SingleOrDefault();


    var orderDetailsWithCustomerFromSp = connection.Query<Order, Customer, Order>("GetOrderDetails",
        (order, customer) =>
            {
                order.Customer = customer;
                order.CustomerId = customer.CustomerId;

                return order;
            },
        param: parameters,
        splitOn: "CustomerId",
        commandType: CommandType.StoredProcedure).SingleOrDefault();


    var sqlOrderDetailsWithOrderItems = @"SELECT 
            o.OrderId, o.OrderDetails, 
            c.CustomerId, c.CustomerName, 
            od.OrderItemId, od.ProductName, od.ProductQuantity 
          FROM Orders o
          INNER JOIN Customers c ON o.CustomerId = c.CustomerId
          INNER JOIN OrderItems od ON o.OrderId = od.OrderId
          WHERE o.OrderId = @OrderId";

    var orderDictionary = new Dictionary<int, Order>();

    var orderDetailsWithOrderItems = connection.Query<Order, Customer, OrderItem, Order>(
        sqlOrderDetailsWithOrderItems,
        (order, customer, orderItem) =>
        {
            Order orderEntry;

            if (!orderDictionary.TryGetValue(order.OrderId, out orderEntry))
            {
                orderEntry = order;
                orderEntry.Items = new List<OrderItem>();
                orderEntry.CustomerId = customer.CustomerId;

                orderDictionary.Add(orderEntry.OrderId, orderEntry);
            }

            if (orderEntry.Customer == null)
                orderEntry.Customer = customer;

            orderItem.OrderId = order.OrderId;

            orderEntry.Items.Add(orderItem);

            return orderEntry;
        },
        new { OrderId = 2 },
        splitOn: "CustomerId,OrderItemId"
        )
        .Distinct()
        .SingleOrDefault();

    var sqlDelete = "DELETE FROM Orders WHERE OrderId = @OrderId";

    connection.Execute(sqlDelete, new { OrderId = id });
}

Console.Read();

public class Order
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public string OrderDetails { get; set; }
    public Customer Customer { get; set; }
    public List<OrderItem> Items { get; set; }
}

public class Customer
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; }
}

public class OrderItem
{
    public int OrderItemId { get; set; }
    public string ProductName { get; set; }
    public int ProductQuantity { get; set; }
    public int OrderId { get; set; }
}

public class OrderDetailResult
{
    public int OrderId { get; set; }
    public string OrderDetails { get; set; }
    public string CustomerId { get; set; }
    public string CustomerName { get; set; }
}
