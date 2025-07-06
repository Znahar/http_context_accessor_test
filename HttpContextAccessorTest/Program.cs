var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<Observer>();
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
}

public static class SamlSubjectStub
{
    public static Observer observer;
}