using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolicyAPI.Data;

namespace PolicyAPI.Controllers
{
    [Route("api")]
    [ApiController]
    public class PolicyPortalController : ControllerBase
    {
        private readonly PolicyDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PolicyPortalController(PolicyDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // ==========================================
        // 1. REGISTRATION & LOGIN
        // ==========================================

        [HttpPost("auth/register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var empId = request.EmployeeId.Trim().ToUpper();
            var email = request.EmailId.Trim().ToLower();

            var exists = await _context.Registrations
                .AnyAsync(u => u.EmployeeId.ToUpper() == empId || u.EmailId.ToLower() == email);

            if (exists)
            {
                return Ok(new { status = "error", message = "An account with this Employee ID or Email already exists!" });
            }

            // Check whether the code is correct or not for the selected department
            var expectedCodeObj = await _context.RegistrationCodes
                .FirstOrDefaultAsync(c => c.Department.ToLower() == request.Department.Trim().ToLower());
            if (expectedCodeObj != null)
            {
                if (expectedCodeObj.Code.Trim().ToUpper() != request.RegistrationCode.Trim().ToUpper())
                {
                    return Ok(new { status = "error", message = $"Incorrect registration code for {request.Department}!" });
                }
            }

            byte roleId = request.RoleId ?? 2;

            var newUser = new Registration
            {
                EmployeeId = request.EmployeeId.Trim(),
                Name = request.Name.Trim(),
                Department = request.Department.Trim(),
                Designation = request.Designation.Trim(),
                EmailId = request.EmailId.Trim(),
                ContactNo = request.ContactNo.Trim(),
                RegistrationCode = request.RegistrationCode.Trim(),
                PasswordHash = request.Password.Trim(), // Plain text to match their current sheets database
                RoleId = roleId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Registrations.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { status = "success", message = "Registered successfully" });
        }

        [HttpPost("auth/login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var empId = request.EmployeeId.Trim().ToUpper();
            var password = request.Password.Trim();

            var user = await _context.Registrations
                .FirstOrDefaultAsync(u => (u.EmployeeId.ToUpper() == empId || u.EmailId.ToUpper() == empId) && u.PasswordHash == password);

            if (user != null)
            {
                if (!user.IsActive)
                {
                    return Ok(new { status = "error", message = "Access Revoked: Your portal deactivation status is active. Please contact administration." });
                }

                var roleName = await _context.Roles
                    .Where(r => r.RoleId == user.RoleId)
                    .Select(r => r.RoleName)
                    .FirstOrDefaultAsync() ?? "Faculty";

                return Ok(new {
                    status = "success",
                    user = new {
                        employeeId = user.EmployeeId,
                        name = user.Name,
                        department = user.Department,
                        designation = user.Designation,
                        roleId = user.RoleId,
                        roleName = roleName
                    }
                });
            }

            return Ok(new { status = "error", message = "Invalid Employee ID or Password" });
        }

        [HttpPost("auth/log-login")]
        public async Task<IActionResult> LogLogin([FromBody] LogLoginRequest request)
        {
            var session = new LastLogin
            {
                EmployeeId = request.EmployeeId.Trim(),
                LoginTime = DateTime.UtcNow
            };

            _context.LastLogins.Add(session);
            await _context.SaveChangesAsync();

            return Ok(new { status = "success", message = "Last login recorded" });
        }

        [HttpPost("profile/update")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var empId = request.EmployeeId.Trim().ToUpper();
            var user = await _context.Registrations
                .FirstOrDefaultAsync(u => u.EmployeeId.ToUpper() == empId);

            if (user == null)
            {
                return Ok(new { status = "error", message = "Employee ID not found" });
            }

            user.Department = request.Department.Trim();
            user.Designation = request.Designation.Trim();
            if (!string.IsNullOrEmpty(request.Password))
            {
                user.PasswordHash = request.Password.Trim();
            }
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { status = "success", message = "Profile updated successfully" });
        }

        // ==========================================
        // 2. POLICY MANAGEMENT
        // ==========================================

        [HttpGet("policies")]
        public async Task<IActionResult> GetPolicies()
        {
            var list = await _context.Policies.Where(p => p.IsActive).ToListAsync();
            var dataList = list.Select(p => new {
                policyTitle = p.PolicyTitle,
                category = p.Category,
                version = p.Version,
                lastUploaded = p.LastUploaded.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                lastUpdated = p.LastUpdated.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                policyDocumentLink = p.PolicyDocumentLink,
                overview = p.Overview,
                faqFileId = p.FaqFileId ?? "",
                quizFileId = p.QuizFileId ?? ""
            }).ToList();

            return Ok(new { status = "success", data = dataList });
        }

        [HttpPost("policies")]
        public async Task<IActionResult> AddPolicy([FromBody] AddPolicyRequest request)
        {
            var title = request.PolicyTitle.Trim();
            var exists = await _context.Policies.AnyAsync(p => p.PolicyTitle == title);
            if (exists)
            {
                return Ok(new { status = "error", message = "A policy with this title already exists" });
            }

            var pdfLink = "";
            if (!string.IsNullOrEmpty(request.PdfContent))
            {
                pdfLink = SaveBase64File(request.Category, title, request.PdfContent, request.PdfName) ?? "";
            }

            var faqId = "";
            if (!string.IsNullOrEmpty(request.FaqHtml))
            {
                faqId = SaveTextFile(request.Category, title, request.FaqHtml, "faq.html") ?? "";
            }

            var quizId = "";
            if (!string.IsNullOrEmpty(request.QuizHtml))
            {
                quizId = SaveTextFile(request.Category, title, request.QuizHtml, "quiz.html") ?? "";
            }

            var newPolicy = new Policy
            {
                PolicyTitle = title,
                Category = request.Category.Trim(),
                Version = request.Version.Trim(),
                Overview = request.Overview.Trim(),
                PolicyDocumentLink = pdfLink,
                FaqFileId = faqId,
                QuizFileId = quizId,
                LastUploaded = request.LastUploaded ?? DateTime.UtcNow,
                LastUpdated = request.LastUpdated ?? DateTime.UtcNow,
                IsActive = true
            };

            _context.Policies.Add(newPolicy);
            await _context.SaveChangesAsync();

            return Ok(new { status = "success", message = "Policy added successfully" });
        }

        [HttpPut("policies/{originalTitle}")]
        public async Task<IActionResult> UpdatePolicy(string originalTitle, [FromBody] UpdatePolicyRequest request)
        {
            var policy = await _context.Policies
                .FirstOrDefaultAsync(p => p.PolicyTitle.ToUpper() == originalTitle.Trim().ToUpper());

            if (policy == null)
            {
                return Ok(new { status = "error", message = "Policy not found" });
            }

            var pdfLink = policy.PolicyDocumentLink;
            if (!string.IsNullOrEmpty(request.PdfContent))
            {
                pdfLink = SaveBase64File(request.Category, request.PolicyTitle, request.PdfContent, request.PdfName) ?? policy.PolicyDocumentLink;
            }

            var faqId = policy.FaqFileId;
            if (!string.IsNullOrEmpty(request.FaqHtml))
            {
                faqId = SaveTextFile(request.Category, request.PolicyTitle, request.FaqHtml, "faq.html") ?? policy.FaqFileId;
            }

            var quizId = policy.QuizFileId;
            if (!string.IsNullOrEmpty(request.QuizHtml))
            {
                quizId = SaveTextFile(request.Category, request.PolicyTitle, request.QuizHtml, "quiz.html") ?? policy.QuizFileId;
            }

            policy.PolicyTitle = request.PolicyTitle.Trim();
            policy.Category = request.Category.Trim();
            policy.Version = request.Version.Trim();
            policy.Overview = request.Overview.Trim();
            policy.PolicyDocumentLink = pdfLink;
            policy.FaqFileId = faqId;
            policy.QuizFileId = quizId;
            if (request.LastUploaded.HasValue)
            {
                policy.LastUploaded = request.LastUploaded.Value;
            }
            policy.LastUpdated = request.LastUpdated ?? DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { status = "success", message = "Policy updated successfully" });
        }

        [HttpDelete("policies/{title}")]
        public async Task<IActionResult> DeletePolicy(string title)
        {
            var policy = await _context.Policies
                .FirstOrDefaultAsync(p => p.PolicyTitle.ToUpper() == title.Trim().ToUpper());

            if (policy == null)
            {
                return Ok(new { status = "error", message = "Policy not found" });
            }

            policy.IsActive = false; // Soft delete
            await _context.SaveChangesAsync();
            return Ok(new { status = "success", message = "Policy deleted successfully" });
        }

        [HttpGet("policies/resource")]
        public IActionResult GetResource([FromQuery] string fileId)
        {
            if (string.IsNullOrWhiteSpace(fileId)) return BadRequest("Resource path is required");

            var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var rootPath = Path.GetFullPath(webRoot);
            var path = Path.GetFullPath(Path.Combine(rootPath, fileId.TrimStart('/', '\\')));
            if (!path.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(path))
            {
                return NotFound("Resource not found");
            }

            var extension = Path.GetExtension(path).ToLowerInvariant();
            var contentType = extension switch
            {
                ".html" => "text/html; charset=utf-8",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
            return PhysicalFile(path, contentType, enableRangeProcessing: true);
        }

        // ==========================================
        // 3. ACKNOWLEDGEMENT & QUIZ ATTEMPTS
        // ==========================================

        [HttpPost("acknowledgements")]
        public async Task<IActionResult> Acknowledge([FromBody] AcknowledgeRequest request)
        {
            var empId = request.EmployeeId.Trim().ToUpper();
            var title = request.PolicyTitle.Trim();

            var exists = await _context.Statuses
                .AnyAsync(s => s.EmployeeId.ToUpper() == empId && s.PolicyTitle == title && s.Acknowledged == "Yes");

            if (exists)
            {
                return Ok(new { status = "success", message = "Policy already acknowledged" });
            }

            var ack = new Status
            {
                EmployeeId = request.EmployeeId.Trim(),
                Name = request.Name.Trim(),
                Department = request.Department.Trim(),
                Category = request.Category.Trim(),
                PolicyTitle = title,
                Acknowledged = "Yes",
                DateAcknowledged = DateTime.UtcNow
            };

            _context.Statuses.Add(ack);
            await _context.SaveChangesAsync();

            return Ok(new { status = "success", message = "Acknowledged successfully" });
        }

        [HttpPost("quizzes/submit")]
        public async Task<IActionResult> SubmitQuiz([FromBody] SubmitQuizRequest request)
        {
            var empId = request.EmployeeId.Trim().ToUpper();
            var title = request.QuizTitle.Trim();
            var score = request.Score;

            var record = await _context.Quizzes
                .FirstOrDefaultAsync(q => q.EmployeeId.ToUpper() == empId && q.QuizTitle == title);

            if (record != null)
            {
                if (record.Attempt1 == null) record.Attempt1 = score;
                else if (record.Attempt2 == null) record.Attempt2 = score;
                else if (record.Attempt3 == null) record.Attempt3 = score;
                else if (record.Attempt4 == null) record.Attempt4 = score;
                else if (record.Attempt5 == null) record.Attempt5 = score;
                else if (record.Attempt6 == null) record.Attempt6 = score;
                else if (record.Attempt7 == null) record.Attempt7 = score;
                else if (record.Attempt8 == null) record.Attempt8 = score;
                else if (record.Attempt9 == null) record.Attempt9 = score;
                else if (record.Attempt10 == null) record.Attempt10 = score;
                else record.Attempt10 = score; // Overwrite last attempt

                if (score > record.BestScore)
                {
                    record.BestScore = score;
                }
                record.DateAttempted = DateTime.UtcNow;
            }
            else
            {
                record = new Quiz
                {
                    EmployeeId = request.EmployeeId.Trim(),
                    QuizTitle = title,
                    Attempt1 = score,
                    BestScore = score,
                    DateAttempted = DateTime.UtcNow
                };
                _context.Quizzes.Add(record);
            }

            await _context.SaveChangesAsync();
            return Ok(new { status = "success", message = "Quiz attempt logged successfully" });
        }

        // ==========================================
        // 4. ADMIN VIEW ALL
        // ==========================================

        [HttpGet("admin/registrations")]
        public async Task<IActionResult> GetRegistrations()
        {
            var list = await _context.Registrations.ToListAsync();
            var dataList = list.Select(u => new {
                employeeId = u.EmployeeId,
                name = u.Name,
                department = u.Department,
                designation = u.Designation,
                email = u.EmailId,
                contactNo = u.ContactNo,
                registrationCode = u.RegistrationCode,
                date = u.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                password = u.PasswordHash,
                isActive = u.IsActive
            }).ToList();

            return Ok(new { status = "success", data = dataList });
        }

        [HttpPut("admin/registrations/{employeeId}")]
        public async Task<IActionResult> UpdateRegistration(string employeeId, [FromBody] RegisterRequest request)
        {
            var user = await _context.Registrations
                .FirstOrDefaultAsync(u => u.EmployeeId.ToUpper() == employeeId.Trim().ToUpper());

            if (user == null)
            {
                return Ok(new { status = "error", message = "Employee ID not found" });
            }

            user.Name = request.Name.Trim();
            user.Department = request.Department.Trim();
            user.Designation = request.Designation.Trim();
            user.EmailId = request.EmailId.Trim();
            user.ContactNo = request.ContactNo.Trim();
            user.RegistrationCode = request.RegistrationCode.Trim();
            user.PasswordHash = request.Password.Trim();
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { status = "success", message = "Updated successfully" });
        }

        [HttpPost("admin/registrations/{employeeId}/toggle-status")]
        public async Task<IActionResult> ToggleRegistrationStatus(string employeeId)
        {
            var user = await _context.Registrations
                .FirstOrDefaultAsync(u => u.EmployeeId.ToUpper() == employeeId.Trim().ToUpper());

            if (user == null)
            {
                return Ok(new { status = "error", message = "Employee ID not found" });
            }

            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { 
                status = "success", 
                message = user.IsActive ? "Portal access granted successfully" : "Portal access revoked successfully",
                isActive = user.IsActive
            });
        }

        [HttpDelete("admin/registrations/{employeeId}")]
        public async Task<IActionResult> DeleteRegistration(string employeeId)
        {
            var user = await _context.Registrations
                .FirstOrDefaultAsync(u => u.EmployeeId.ToUpper() == employeeId.Trim().ToUpper());

            if (user == null)
            {
                return Ok(new { status = "error", message = "Employee ID not found" });
            }

            _context.Registrations.Remove(user);
            await _context.SaveChangesAsync();
            return Ok(new { status = "success", message = "Access revoked successfully" });
        }

        [HttpGet("quizzes/{employeeId}")]
        public async Task<IActionResult> GetUserQuizzes(string employeeId)
        {
            var empId = employeeId.Trim().ToUpper();
            var list = await _context.Quizzes
                .Where(q => q.EmployeeId.ToUpper() == empId)
                .ToListAsync();

            var dataList = list.Select(q => new {
                employeeId = q.EmployeeId,
                quizTitle = q.QuizTitle,
                attempt1 = q.Attempt1,
                attempt2 = q.Attempt2,
                attempt3 = q.Attempt3,
                attempt4 = q.Attempt4,
                attempt5 = q.Attempt5,
                attempt6 = q.Attempt6,
                attempt7 = q.Attempt7,
                attempt8 = q.Attempt8,
                attempt9 = q.Attempt9,
                attempt10 = q.Attempt10,
                bestScore = q.BestScore,
                date = q.DateAttempted.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            }).ToList();

            return Ok(new { status = "success", data = dataList });
        }

        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var list = await _context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            var dataList = list.Select(n => new {
                notificationId = n.NotificationId,
                message = n.Message,
                date = n.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            }).ToList();

            return Ok(new { status = "success", data = dataList });
        }

        [HttpPost("notifications")]
        public async Task<IActionResult> CreateNotification([FromBody] NewNotificationRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return Ok(new { status = "error", message = "Message content is required" });
            }

            var notification = new Notification
            {
                Message = request.Message.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return Ok(new { status = "success", message = "Notification sent successfully" });
        }

        [HttpGet("admin/quizzes")]
        public async Task<IActionResult> GetQuizzes()
        {
            var list = await _context.Quizzes.ToListAsync();
            var dataList = list.Select(q => new {
                employeeId = q.EmployeeId,
                quizTitle = q.QuizTitle,
                attempt1 = q.Attempt1?.ToString() ?? "",
                attempt2 = q.Attempt2?.ToString() ?? "",
                attempt3 = q.Attempt3?.ToString() ?? "",
                attempt4 = q.Attempt4?.ToString() ?? "",
                attempt5 = q.Attempt5?.ToString() ?? "",
                attempt6 = q.Attempt6?.ToString() ?? "",
                attempt7 = q.Attempt7?.ToString() ?? "",
                attempt8 = q.Attempt8?.ToString() ?? "",
                attempt9 = q.Attempt9?.ToString() ?? "",
                attempt10 = q.Attempt10?.ToString() ?? "",
                bestScore = q.BestScore,
                date = q.DateAttempted.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            }).ToList();

            return Ok(new { status = "success", data = dataList });
        }

        [HttpGet("admin/acknowledgements")]
        public async Task<IActionResult> GetAcknowledgements()
        {
            var list = await _context.Statuses.ToListAsync();
            var dataList = list.Select(s => new {
                employeeId = s.EmployeeId,
                name = s.Name,
                department = s.Department,
                category = s.Category,
                policyTitle = s.PolicyTitle,
                acknowledged = s.Acknowledged,
                date = s.DateAcknowledged.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            }).ToList();

            return Ok(new { status = "success", data = dataList });
        }

        // ==========================================
        // PRIVATE HELPERS
        // ==========================================

        private string? SaveBase64File(string category, string title, string? base64Content, string fileName)
        {
            if (string.IsNullOrEmpty(base64Content)) return null;

            try
            {
                var safeTitle = string.Concat(title.Split(Path.GetInvalidFileNameChars()));
                var safeCategory = string.Concat(category.Split(Path.GetInvalidFileNameChars()));

                var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
                var folderPath = Path.Combine(webRoot, "policies", safeCategory, safeTitle);
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var safeFileName = Path.GetFileName(fileName);
                if (string.IsNullOrWhiteSpace(safeFileName)) safeFileName = "policy.pdf";
                var filePath = Path.Combine(folderPath, safeFileName);
                var commaIndex = base64Content.IndexOf(',');
                var rawBase64 = commaIndex >= 0 ? base64Content[(commaIndex + 1)..] : base64Content;
                var bytes = Convert.FromBase64String(rawBase64);
                System.IO.File.WriteAllBytes(filePath, bytes);

                return $"/policies/{safeCategory}/{safeTitle}/{safeFileName}";
            }
            catch
            {
                return null;
            }
        }

        private string? SaveTextFile(string category, string title, string? content, string fileName)
        {
            if (string.IsNullOrEmpty(content)) return null;

            try
            {
                var safeTitle = string.Concat(title.Split(Path.GetInvalidFileNameChars()));
                var safeCategory = string.Concat(category.Split(Path.GetInvalidFileNameChars()));

                var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
                var folderPath = Path.Combine(webRoot, "policies", safeCategory, safeTitle);
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var filePath = Path.Combine(folderPath, fileName);
                System.IO.File.WriteAllText(filePath, content);

                return $"/policies/{safeCategory}/{safeTitle}/{fileName}";
            }
            catch
            {
                return null;
            }
        }

        // ==========================================
        // 4. ROLES & DESIGNATIONS
        // ==========================================

        [HttpGet("admin/roles")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _context.Roles.OrderBy(r => r.RoleId).ToListAsync();
            return Ok(new { status = "success", data = roles });
        }

        [HttpPost("admin/roles")]
        public async Task<IActionResult> AddRole([FromBody] AddRoleRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RoleName)) 
                return Ok(new { status = "error", message = "Role name is required" });

            var exists = await _context.Roles.AnyAsync(r => r.RoleName.ToLower() == request.RoleName.Trim().ToLower());
            if (exists)
                return Ok(new { status = "error", message = "Role already exists" });

            var newRole = new Role { RoleName = request.RoleName.Trim() };
            _context.Roles.Add(newRole);
            await _context.SaveChangesAsync();
            return Ok(new { status = "success", message = "Role added successfully" });
        }

        [HttpGet("admin/designations")]
        public async Task<IActionResult> GetDesignations()
        {
            var designations = await _context.Designations.OrderBy(d => d.DesignationName).ToListAsync();
            return Ok(new { status = "success", data = designations });
        }

        [HttpPost("admin/designations")]
        public async Task<IActionResult> AddDesignation([FromBody] AddDesignationRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.DesignationName)) 
                return Ok(new { status = "error", message = "Designation name is required" });

            var exists = await _context.Designations.AnyAsync(d => d.DesignationName.ToLower() == request.DesignationName.Trim().ToLower());
            if (exists)
                return Ok(new { status = "error", message = "Designation already exists" });

            var newDes = new Designation { DesignationName = request.DesignationName.Trim() };
            _context.Designations.Add(newDes);
            await _context.SaveChangesAsync();
            return Ok(new { status = "success", message = "Designation added successfully" });
        }

        [HttpGet("admin/registration-codes")]
        public async Task<IActionResult> GetRegistrationCodes()
        {
            var codes = await _context.RegistrationCodes.OrderBy(c => c.Department).ToListAsync();
            return Ok(new { status = "success", data = codes });
        }

        [HttpPost("admin/registration-codes/batch")]
        public async Task<IActionResult> SaveRegistrationCodesBatch([FromBody] List<RegistrationCodeMap> codes)
        {
            if (codes == null)
                return Ok(new { status = "error", message = "No data provided" });

            // Remove all existing registration codes
            _context.RegistrationCodes.RemoveRange(_context.RegistrationCodes);
            await _context.SaveChangesAsync();

            // Insert the new batch
            foreach (var code in codes)
            {
                code.Id = 0; // reset identity column
                if (!string.IsNullOrWhiteSpace(code.Department) && !string.IsNullOrWhiteSpace(code.Code))
                {
                    _context.RegistrationCodes.Add(code);
                }
            }
            await _context.SaveChangesAsync();
            return Ok(new { status = "success", message = "Registration codes updated successfully" });
        }
    }

    // ==========================================
    // REQUEST CLASS DEFINITIONS
    // ==========================================

    public class RegisterRequest
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public byte? RoleId { get; set; }
        public string EmailId { get; set; } = string.Empty;
        public string ContactNo { get; set; } = string.Empty;
        public string RegistrationCode { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LogLoginRequest
    {
        public string EmployeeId { get; set; } = string.Empty;
    }

    public class UpdateProfileRequest
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string? Password { get; set; }
    }

    public class AddPolicyRequest
    {
        public string PolicyTitle { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string? PdfContent { get; set; }
        public string PdfName { get; set; } = string.Empty;
        public string? FaqHtml { get; set; }
        public string? QuizHtml { get; set; }
        public string Overview { get; set; } = string.Empty;
        public DateTime? LastUploaded { get; set; }
        public DateTime? LastUpdated { get; set; }
    }

    public class UpdatePolicyRequest : AddPolicyRequest
    {
        public string OriginalTitle { get; set; } = string.Empty;
    }

    public class AcknowledgeRequest
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string PolicyTitle { get; set; } = string.Empty;
    }

    public class SubmitQuizRequest
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string QuizTitle { get; set; } = string.Empty;
        public decimal Score { get; set; }
    }

    public class NewNotificationRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class AddRoleRequest
    {
        public string RoleName { get; set; } = string.Empty;
    }

    public class AddDesignationRequest
    {
        public string DesignationName { get; set; } = string.Empty;
    }
}
