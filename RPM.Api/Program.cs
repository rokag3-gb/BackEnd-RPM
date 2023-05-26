using RPM.Infra.Data;
using RPM.Api.App.Repository;
using RPM.Api.App.Queries;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using RPM.Domain.Mappers;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy  =>
                      {
                          policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                      });
});

builder.Services.AddTransient<RPMDbConnection>();
builder.Services.AddScoped<ICredentialQueries, CredentialQueries>();
builder.Services.AddScoped<ICredentialRepository, CredentialRepository>();
builder.Services.AddScoped<IInstanceQueries, InstanceQueries>();
builder.Services.AddScoped<IInstanceRepository, InstanceRepository>();
// Add services to the container.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(
        JwtBearerDefaults.AuthenticationScheme,
        options =>
        {
            options.Authority = configuration.GetValue<string>("Jwt:Issuer");
            options.Audience = configuration.GetValue<string>("Jwt:Audience");
        }
    );
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
    c.SupportNonNullableReferenceTypes();
    c.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Description =
                @"Header 의 Authorizationdp 들어갈 JWT Bearer 인가 토큰.
        (예시: `eyJ...In0.eyJ...CJ9.ZLo...IDQ`)",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer"
        }
    );
    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] { }
            }
        }
    );
    //c.OperationFilter<AuthResponsesOperationFilter>();
});
builder.Services.AddAutoMapper(typeof(CredentialMapperProfile).Assembly);

var app = builder.Build();
app.UseCors(MyAllowSpecificOrigins);

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
app.UseSwagger();
app.UseSwaggerUI();

// }

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
