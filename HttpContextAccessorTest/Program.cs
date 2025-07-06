using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<Observer>();
builder.Services.AddTransient<SessionStoreMock>();
var app = builder.Build();

SamlSubjectStub.observer = app.Services.GetRequiredService<Observer>();


app.MapGet("/logRequest", (IHttpContextAccessor contextAccessor) =>
{
    var sessIdFromRequest = contextAccessor.HttpContext!.Request.Headers["sess-id"].ToString();
    SamlSubjectStub.observer.Observe(sessIdFromRequest);
    for (int i = 0; i < 2000; i++) new Random().Next();
    Thread.Sleep(10);

    var iterationId = int.Parse(sessIdFromRequest.Split('-')[1]);
    if(iterationId % 200 == 0)
    {
        Console.WriteLine($"processing request {sessIdFromRequest}");
    }

    return SamlSubjectStub.observer.Observe(sessIdFromRequest);
});

app.MapGet("/observeScope", (IHttpContextAccessor contextAccessor) =>
{
    var sessIdFromRequest = contextAccessor.HttpContext!.Request.Headers["sess-id"].ToString();
    SamlSubjectStub.observer.ObserveScope(sessIdFromRequest);
    for (int i = 0; i < 2000; i++) new Random().Next();
    Thread.Sleep(10);

    var iterationId = int.Parse(sessIdFromRequest.Split('-')[1]);
    if (iterationId % 200 == 0)
    {
        Console.WriteLine($"os processing request {sessIdFromRequest}");
    }

    return SamlSubjectStub.observer.ObserveScope(sessIdFromRequest);
});


app.Run();

public class Observer(IHttpContextAccessor httpContextAccessor) { 
    public bool Observe(string sessIdFromRequest)
    {
        if (httpContextAccessor is null || httpContextAccessor.HttpContext is null)
            throw new Exception($"Sess Id form context is null. Sess Id from request: {sessIdFromRequest}");

        var sessIdFromContext = httpContextAccessor.HttpContext?.Request?.Headers?["sess-id"];

        if (sessIdFromContext is null)
            throw new Exception($"Sess Id form context is null. Sess Id from request: {sessIdFromRequest}");

        if (sessIdFromContext.ToString() != sessIdFromRequest)
            throw new Exception($"Context sessid({sessIdFromContext}) does not match sessidFromRequest {sessIdFromRequest}");


        return sessIdFromRequest == sessIdFromContext.ToString();
    }

    public bool ObserveScope(string sessIdFromRequest)
    {

        if (httpContextAccessor is null)
            throw new Exception($"Sess Id form context is null. Sess Id from request: {sessIdFromRequest}");
        var errors = new List<string>();
        string sessIdFromContext = "";
        Task.Run(() =>
        {
            using var scope = httpContextAccessor.HttpContext?.RequestServices.CreateScope();
            var sessIdFromSessionStore = scope.ServiceProvider.GetRequiredService<SessionStoreMock>().GetSessionId();
            sessIdFromContext = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext?.Request?.Headers?["sess-id"];


            if (sessIdFromContext is null)
                errors.Add($"Sess Id form context is null. Sess Id from request: {sessIdFromRequest}");

            if (sessIdFromContext.ToString() != sessIdFromRequest)
                errors.Add($"Context sessid({sessIdFromContext}) does not match sessidFromRequest {sessIdFromRequest}");

            if (sessIdFromSessionStore is null)
                errors.Add($"Sess Id from store is null. Sess Id from request: {sessIdFromRequest}");

            if (sessIdFromSessionStore != sessIdFromRequest)
                errors.Add($"Context sessid({sessIdFromSessionStore}) does not match sessidFromRequest {sessIdFromRequest}");

            if (sessIdFromSessionStore != sessIdFromContext.ToString())
                errors.Add($"Context sessid({sessIdFromSessionStore}) does not match sessidFromContext {sessIdFromContext.ToString()}");
        }).Wait();
        if (errors.Any())
            throw new Exception(string.Join(';', errors));

        return sessIdFromRequest == sessIdFromContext.ToString();
    }
}

public static class SamlSubjectStub
{
    public static Observer observer;
}

public class SessionStoreMock(IHttpContextAccessor contextAccessor)
{
    public string sessID;
    public string GetSessionId()
    {
        if (string.IsNullOrEmpty(sessID))
            sessID = contextAccessor.HttpContext.Request.Headers["sess-id"];
        return sessID;
    }
}