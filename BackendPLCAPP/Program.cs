using BackendPLCAPP; // 0. 파일이 있는 네임스페이스
using BackendPLCAPP.Controllers;
using BackendPLCAPP.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<C6015Work>(); // 1. 클래스를 싱글톤으로 등록
builder.Services.AddSingleton<DemoRoutineService>(); // 데모 루틴 서비스를 싱글톤으로 추가

builder.Services.AddControllers();
// 2. CORS 설정 (프론트엔드 React가 접근할 수 있도록 허용)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Vite 기본 포트
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // SSE 연결 시 필요할 수 있음
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(); // 3. CORS 적용

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
