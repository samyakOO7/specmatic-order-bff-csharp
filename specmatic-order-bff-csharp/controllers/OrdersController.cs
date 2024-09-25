using Microsoft.AspNetCore.Mvc;
using specmatic_order_bff_csharp.models;
using specmatic_order_bff_csharp.services;

namespace specmatic_order_bff_csharp.controllers;

[ApiController]
[Route("/order")]
public class OrdersController(IOrderBFFService orderBffService) : ControllerBase
{
    [HttpPost]
    public IActionResult CreateOrder([FromBody]OrderRequest orderRequest)
    {
        return StatusCode(StatusCodes.Status201Created, orderBffService.CreateOrder(orderRequest));
    }
}