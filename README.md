# Order Processing & Inventory Management System

## Overview

This project is a simple **e-commerce system** where customers can place orders for products, and the system manages inventory and processes orders asynchronously.

## Getting Started

1. **Open the project**:
   - Open the `OrderProcessingSystem.sln` file in **Visual Studio**.

2. **Run the application**:
   - Press **F5** or click **Run** from the toolbar.
   - The application will start locally, and you can view the **Swagger UI** at `https://localhost:7255/swagger/index.html` in your browser.

3. **Using the Swagger UI**:

   You can test the following API endpoints in the given order:

   - **1. Add a Product**:  
     - **POST /api/Product**  
     - **Input**: Provide JSON data with the following fields:
       ```json
       {
         "name": "Guitar",
         "price": 500,
         "stockQuantity": 10
       }
       ```

   - **2. View All Products**:  
     - **GET /api/Product**  
     - This will retrieve a list of all products.

   - **3. View a Product by ID**:  
     - **GET /api/Product/{id}**  
     - **Input**: Enter the product `id` in the URL to view specific product details.

   - **4. Update a Product**:  
     - **PUT /api/Product/{id}**  
     - **Input**: Provide updated JSON data for the product:
       ```json
       {
         "name": "Electric Guitar",
         "price": 600,
         "stockQuantity": 15
       }
       ```

   - **5. Delete a Product**:  
     - **DELETE /api/Product/{id}**  
     - **Input**: Enter the product `id` in the URL to delete the product.

   - **6. Restock a Product**:  
     - **POST /api/Product/{id}/restock**  
     - **Input**: Provide the quantity to restock the product as a query parameter:
       - Example URL: `https://localhost:7255/api/Product/1/restock?quantity=5`

   - **7. Place an Order**:  
     - **POST /api/Order**  
     - **Input**: Provide a JSON body with the order details, including `OrderItems`:
       ```json
 [
    { "productId": 1, "quantity": 2 },
    { "productId": 2, "quantity": 1 }
]

       ```

   - **8. View an Order by ID**:  
     - **GET /api/Order/{id}**  
     - **Input**: Enter the order `id` in the URL to view the order details.

   - **9. Cancel an Order**:  
     - **POST /api/Order/{id}/cancel**  
     - **Input**: Enter the order `id` in the URL to cancel the order.

## Running Unit Tests

1. **Open Test Explorer**:
   - Go to **Test** → **Windows** → **Test Explorer** in Visual Studio.

2. **Run all tests**:
   - In the **Test Explorer** window, click **Run All** to execute all the unit tests.
   - This will run tests for:
     - **Order Placement**: Valid order placement with sufficient stock.
     - **Insufficient Stock**: Order fails when stock is insufficient.
     - **Order Cancellation**: Cancel an unfulfilled order and restore stock.
     - **Order Fulfillment**: Verify that an order is marked as fulfilled.
     - **Concurrency Handling**: Simulate two concurrent orders and ensure only one succeeds.
     - **Retry Logic**: Ensure retry attempts are made if fulfillment fails.

___
### Key Design Decisions

- **Separation of Concerns**: Divided the system into clear layers (controllers, services, data access) for better maintainability and scalability.
- **Dependency Injection**: Used to decouple components, making the system modular and easier to test.
- **SOLID Principles**: Ensured each class has a single responsibility, improving flexibility and reducing the impact of changes.
- **Testability**: The design allows easy substitution of components with mocks or stubs for unit testing, ensuring reliable tests.
___
### Handling Concurrency and Asynchronous Processing

- **Concurrency Handling**:
  - Used **thread-safe operations** with locks to ensure inventory updates are correct when multiple orders are placed for the same product.
  - Prevents race conditions and ensures data consistency during simultaneous transactions.

- **Asynchronous Processing**:
  - Order fulfillment is processed **asynchronously** using a background service, preventing UI or other API requests from being blocked.
  - Implements a **retry mechanism** to handle temporary failures during order processing, improving system reliability.

