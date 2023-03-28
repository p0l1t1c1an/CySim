namespace CySim.Tests.UnitTests;
using CySim.Controllers;
using CySim.Data;
using CySim.Interfaces;
using CySim.Models.Scenario;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

public class ScenarioControllerTests
{
    private (ApplicationDbContext, ScenarioController, List<Scenario>) InitializeTest()
    {
        // Set up logger and mock db to create controller  
        var logger = NullLogger<ScenarioController>.Instance;
        
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseInMemoryDatabase("CySim");
        
        var context = new ApplicationDbContext(optionsBuilder.Options);

        // Creates database tables and columns if they don't exist
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
       
        // Create mock of File Service
        var fileServiceMock = new Mock<IFileService>();
        fileServiceMock.Setup(_ => _.WriteIFormFile(It.IsAny<IFormFile>(), It.IsAny<String>())).Verifiable();
        fileServiceMock.Setup(_ => _.MoveFile(It.IsAny<String>(), It.IsAny<String>())).Verifiable();
        fileServiceMock.Setup(_ => _.DeleteFile(It.IsAny<String>())).Verifiable();
        
        var controller = new ScenarioController(logger, context, fileServiceMock.Object) 
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
        
        // Initialize scenarios in db
        var scenarios = new List<Scenario>{
            new Scenario{Id = 1, FileName  = "FirstFile", FilePath = "Test/FirstFile", Description = "First Test Scenario", isRed = false}, 
            new Scenario{Id = 2, FileName  = "SecondFile", FilePath = "Test/SecondFile", Description = "Second Test Scenario", isRed = true}, 
            new Scenario{Id = 3, FileName  = "ThirdFile", FilePath = "Test/ThirdFile", Description = "Third Test Scenario", isRed = false} 
        };
        
        context.Scenarios.AddRange(scenarios);
        context.SaveChanges(); 

        return (context, controller, scenarios);
    }

    private static Mock<IFormFile> GenerateMockFile(String fileName)
    {
        //Create mock file
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns(fileName).Verifiable();
        file.Setup(_ => _.CopyTo(It.IsAny<Stream>())).Verifiable();

        return file;
    }


    public static IEnumerable<object[]> GenerateValidCreatePostData()
    {
        yield return new object[] { GenerateMockFile("Fake.pdf"), "Fake Scenario 1 PDF", false };
        yield return new object[] { GenerateMockFile("Fake.pdf"), "Fake Scenario 2 PDF", true };
    }

    public static IEnumerable<object[]> GenerateInvalidCreatePostData()
    {
        yield return new object[] { 5, 1, 3, 9 };
        yield return new object[] { 7, 1, 5, 3 };
    }
       
    [Fact]
    public void IndexGet() 
    { 
        (_, var controller, var scenarios) = InitializeTest(); 

        // Query Index 
        var result = controller.Index();
        
        // Confirm that a view is returned and has correct model type
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<Scenario>>(viewResult.ViewData.Model);
        Assert.Equal(scenarios.OrderBy(x => x.isRed), model);    
    }

    [Fact]
    public void CreateGet() 
    { 
        (_, var controller, _) = InitializeTest(); 

        // Query Index 
        var result = controller.Create();
        
        // Confirm that a view is returned and has correct model type
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.ViewData.Model); // Create Get request has no model
    }

    [Theory]
    [MemberData(nameof(GenerateValidCreatePostData))]
    public void CreatePost_ValidData(Mock<IFormFile> file, String Description, bool isRed) 
    { 
        (_, var controller, _) = InitializeTest(); 

        // Query Index 
        var result = controller.Create(file.Object, Description, isRed);
 
        // Confirm that we redirect to index with invalid ID
        var RedirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Null(RedirectResult.ControllerName);
        Assert.Equal("Index", RedirectResult.ActionName);
    }


    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void EditGet_ValidRequest(int id) 
    { 
        (_, var controller, var scenarios) = InitializeTest(); 

        // Query Index 
        var result = controller.Edit(id);
        
        // Confirm that a view is returned and has correct model type
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<Scenario>(viewResult.ViewData.Model);
  
        /// Confirm Model is the one queried by id number
        Assert.NotNull(model);    
        Assert.Equal(scenarios[id-1], model);
    }

    [Theory]
    [InlineData(100)]
    public void EditGet_NonexistentID(int id) 
    { 
        (_, var controller, var scenarios) = InitializeTest(); 

        // Query Index 
        var result = controller.Edit(id);
        
        // Confirm that we redirect to index with invalid ID
        var RedirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Null(RedirectResult.ControllerName);
        Assert.Equal("Index", RedirectResult.ActionName);
    }

}
