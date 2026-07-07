using Microsoft.EntityFrameworkCore;
using PolicyAPI.Data;

namespace PolicyAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add DbContext
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<PolicyDbContext>(options =>
                options.UseSqlServer(connectionString));

            // Enable CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();
            
            // Add Swagger services
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Create the LocalDB database and schema automatically on first run.
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<PolicyDbContext>();
                dbContext.Database.EnsureCreated();

                dbContext.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Designations' and xtype='U')
                    BEGIN
                        CREATE TABLE Designations (
                            DesignationId INT IDENTITY(1,1) PRIMARY KEY,
                            DesignationName VARCHAR(150) NOT NULL UNIQUE
                        );
                        INSERT INTO Designations (DesignationName) VALUES 
                        ('Assistant Professor'),
                        ('Associate Professor'),
                        ('Professor'),
                        ('Head of the Department'),
                        ('Dean - Academics'),
                        ('Dean - Research'),
                        ('Accountant'),
                        ('Lab Instructor'),
                        ('System Admin'),
                        ('IT Admin'),
                        ('Principal'),
                        ('Administrative Officer'),
                        ('Assistant Administrative Officer');
                    END
                ");

                dbContext.Database.ExecuteSqlRaw(@"
                    ALTER TABLE Roles ALTER COLUMN RoleName VARCHAR(150) NOT NULL;
                ");

                dbContext.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT * FROM Roles WHERE RoleName = 'Head') INSERT INTO Roles (RoleName) VALUES ('Head');
                    IF NOT EXISTS (SELECT * FROM Roles WHERE RoleName = 'Dean - Academics') INSERT INTO Roles (RoleName) VALUES ('Dean - Academics');
                    IF NOT EXISTS (SELECT * FROM Roles WHERE RoleName = 'Dean - Research') INSERT INTO Roles (RoleName) VALUES ('Dean - Research');
                    IF NOT EXISTS (SELECT * FROM Roles WHERE RoleName = 'Accountant') INSERT INTO Roles (RoleName) VALUES ('Accountant');
                    IF NOT EXISTS (SELECT * FROM Roles WHERE RoleName = 'Lab Instructor') INSERT INTO Roles (RoleName) VALUES ('Lab Instructor');
                    IF NOT EXISTS (SELECT * FROM Roles WHERE RoleName = 'System Admin') INSERT INTO Roles (RoleName) VALUES ('System Admin');
                    IF NOT EXISTS (SELECT * FROM Roles WHERE RoleName = 'IT Admin') INSERT INTO Roles (RoleName) VALUES ('IT Admin');
                    IF NOT EXISTS (SELECT * FROM Roles WHERE RoleName = 'Principal') INSERT INTO Roles (RoleName) VALUES ('Principal');
                    IF NOT EXISTS (SELECT * FROM Roles WHERE RoleName = 'Administrative Officer') INSERT INTO Roles (RoleName) VALUES ('Administrative Officer');
                    IF NOT EXISTS (SELECT * FROM Roles WHERE RoleName = 'Assistant Administrative Officer') INSERT INTO Roles (RoleName) VALUES ('Assistant Administrative Officer');
                ");

                dbContext.Database.ExecuteSqlRaw(@"
                    UPDATE Registrations SET Designation = 'Professor', RoleId = 10 WHERE EmployeeId = 'CS00001';
                    UPDATE Registrations SET Designation = 'Professor', RoleId = 3 WHERE EmployeeId = 'CS01101';
                ");

                dbContext.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='RegistrationCodes' and xtype='U')
                    BEGIN
                        CREATE TABLE RegistrationCodes (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            Department VARCHAR(250) NOT NULL UNIQUE,
                            Code VARCHAR(100) NOT NULL
                        );
                        INSERT INTO RegistrationCodes (Department, Code) VALUES 
                        ('Administration', 'ADMIN@ATMECE'),
                        ('Civil Engineering', 'CV@ATMECE'),
                        ('Computer Science and Engineering', 'CSE@ATMECE'),
                        ('CSE – Data Science', 'CSEDS@ATMECE'),
                        ('CSE – Artificial Intelligence and Machine Learning', 'CSEAIML@ATMECE'),
                        ('CSE – Cyber Security', 'CSECY@ATMECE'),
                        ('Computer Science and Design', 'CSECD@ATMECE'),
                        ('Electronics and Communication Engineering', 'ECE@ATMECE'),
                        ('Electrical and Electronics Engineering', 'EEE@ATMECE'),
                        ('Mechanical Engineering', 'ME@ATMECE'),
                        ('Bachelor of Computer Applications', 'BCA@ATMECE'),
                        ('Master of Computer Applications', 'MCA@ATMECE'),
                        ('Master of Business Administration', 'MBA@ATMECE'),
                        ('Chemistry', 'CHE@ATMECE'),
                        ('Mathematics', 'MATH@ATMECE'),
                        ('Physics', 'PHY@ATMECE'),
                        ('Humanities', 'HUM@ATMECE'),
                        ('Library', 'LIB@ATMECE'),
                        ('Physical Education and Sports', 'SPORTS@ATMECE'),
                        ('NSS', 'NSS@ATMECE'),
                        ('Accounts and Finance', 'FIN@ATMECE'),
                        ('Human Resources', 'HR@ATMECE'),
                        ('Placement and Training', 'PLACE@ATMECE'),
                        ('Research and Development', 'RD@ATMECE'),
                        ('Establishment Section', 'EST@ATMECE'),
                        ('Computer Science & Engineering', 'CSE@ATMECE'),
                        ('Computer Science & Engineering - Artificial Intelligence & Machine Learning', 'CSEAIML@ATMECE'),
                        ('Computer Science & Design', 'CSECD@ATMECE'),
                        ('Computer Science & Engineering - Cyber Security', 'CSECY@ATMECE'),
                        ('Computer Science & Engineering - Data Science', 'CSEDS@ATMECE'),
                        ('Electronics & Communication Engineering', 'ECE@ATMECE'),
                        ('Electrical & Electronics Engineering', 'EEE@ATMECE'),
                        ('Humanities (Communication, Language, Soft Skills)', 'HUM@ATMECE');
                    END
                ");
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            // Enable Swagger middleware (accessible at /swagger)
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "ATME Policy API v1");
                c.RoutePrefix = "swagger";
            });

            // Enable Static Files (crucial for local policies directory)
            app.UseStaticFiles();

            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseCors("AllowAll");

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
