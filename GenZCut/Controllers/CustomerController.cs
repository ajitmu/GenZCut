using GenZCut.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace GenZCut.Controllers
{
    public class CustomerController : Controller
    {
        private readonly string _connectionString;

        public CustomerController(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string not found.");
        }
        [HttpGet]
        public IActionResult ExploreServices()
        {
            return View();
        }

        // =====================================================
        // CUSTOMER REGISTRATION - GET
        // =====================================================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }


        // =====================================================
        // CUSTOMER REGISTRATION - POST (WITH PASSWORD HASHING)
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(Customer customer)
        {
            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            try
            {
                using SqlConnection connection =
                    new SqlConnection(_connectionString);

                connection.Open();

                // ---------------------------------------------
                // CHECK DUPLICATE EMAIL
                // ---------------------------------------------

                string checkEmailQuery = @"
                    SELECT COUNT(*)
                    FROM Users
                    WHERE Email = @Email";

                using SqlCommand checkEmailCommand =
                    new SqlCommand(checkEmailQuery, connection);

                checkEmailCommand.Parameters.AddWithValue(
                    "@Email",
                    customer.Email);

                int emailExists =
                    Convert.ToInt32(
                        checkEmailCommand.ExecuteScalar());

                if (emailExists > 0)
                {
                    ModelState.AddModelError(
                        "Email",
                        "This email is already registered.");

                    return View(customer);
                }


                // ---------------------------------------------
                // CHECK DUPLICATE PHONE
                // ---------------------------------------------

                string checkPhoneQuery = @"
                    SELECT COUNT(*)
                    FROM Users
                    WHERE Phone = @Phone";

                using SqlCommand checkPhoneCommand =
                    new SqlCommand(checkPhoneQuery, connection);

                checkPhoneCommand.Parameters.AddWithValue(
                    "@Phone",
                    customer.Phone);

                int phoneExists =
                    Convert.ToInt32(
                        checkPhoneCommand.ExecuteScalar());

                if (phoneExists > 0)
                {
                    ModelState.AddModelError(
                        "Phone",
                        "This phone number is already registered.");

                    return View(customer);
                }


                // ---------------------------------------------
                // HASH PASSWORD
                // ---------------------------------------------

                var passwordHasher = new PasswordHasher<Customer>();
                string hashedPassword = passwordHasher.HashPassword(customer, customer.Password);


                // ---------------------------------------------
                // INSERT USER AND GET UserId
                // ---------------------------------------------

                string userQuery = @"
                    INSERT INTO Users
                    (
                        FullName,
                        Email,
                        Password,
                        Phone,
                        Role,
                        CreatedDate
                    )
                    OUTPUT INSERTED.UserId
                    VALUES
                    (
                        @FullName,
                        @Email,
                        @Password,
                        @Phone,
                        'Customer',
                        GETDATE()
                    )";

                using SqlCommand userCommand =
                    new SqlCommand(userQuery, connection);

                userCommand.Parameters.AddWithValue(
                    "@FullName",
                    customer.FullName);

                userCommand.Parameters.AddWithValue(
                    "@Email",
                    customer.Email);

                userCommand.Parameters.AddWithValue(
                    "@Password",
                    hashedPassword);

                userCommand.Parameters.AddWithValue(
                    "@Phone",
                    customer.Phone);

                int userId =
                    Convert.ToInt32(
                        userCommand.ExecuteScalar());


                // ---------------------------------------------
                // INSERT CUSTOMER RECORD
                // ---------------------------------------------

                string customerQuery = @"
                    INSERT INTO Customers
                    (
                        UserId,
                        Address,
                        Gender
                    )
                    VALUES
                    (
                        @UserId,
                        @Address,
                        @Gender
                    )";

                using SqlCommand customerCommand =
                    new SqlCommand(customerQuery, connection);

                customerCommand.Parameters.AddWithValue(
                    "@UserId",
                    userId);

                customerCommand.Parameters.AddWithValue(
                    "@Address",
                    string.IsNullOrWhiteSpace(customer.Address)
                        ? DBNull.Value
                        : customer.Address);

                customerCommand.Parameters.AddWithValue(
                    "@Gender",
                    string.IsNullOrWhiteSpace(customer.Gender)
                        ? DBNull.Value
                        : customer.Gender);

                customerCommand.ExecuteNonQuery();


                TempData["Success"] =
                    "Registration successful! Please login.";

                return RedirectToAction("Login");
            }
            catch (SqlException)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to save registration. Please try again.");

                return View(customer);
            }
        }


        // =====================================================
        // CUSTOMER LOGIN - GET
        // =====================================================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }


        // =====================================================
        // CUSTOMER LOGIN - POST (WITH PASSWORD VERIFICATION)
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(
                    "email",
                    "Email is required.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(
                    "password",
                    "Password is required.");
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            try
            {
                using SqlConnection connection =
                    new SqlConnection(_connectionString);

                connection.Open();

                string query = @"
                    SELECT
                        c.CustomerId,
                        u.UserId,
                        u.FullName,
                        u.Email,
                        u.Password
                    FROM Users u
                    INNER JOIN Customers c
                        ON u.UserId = c.UserId
                    WHERE u.Email = @Email
                    AND u.Role = 'Customer'";

                using SqlCommand command =
                    new SqlCommand(query, connection);

                command.Parameters.AddWithValue(
                    "@Email",
                    email);

                using SqlDataReader reader =
                    command.ExecuteReader();

                if (reader.Read())
                {
                    string storedHashedPassword = reader["Password"].ToString() ?? "";

                    var passwordHasher = new PasswordHasher<Customer>();
                    var verificationResult = passwordHasher.VerifyHashedPassword(new Customer(), storedHashedPassword, password);

                    if (verificationResult == PasswordVerificationResult.Success)
                    {
                        int customerId =
                            Convert.ToInt32(
                                reader["CustomerId"]);

                        int userId =
                            Convert.ToInt32(
                                reader["UserId"]);

                        string fullName =
                            reader["FullName"].ToString() ?? "";

                        string customerEmail =
                            reader["Email"].ToString() ?? "";


                        // -----------------------------------------
                        // CUSTOMER SESSION
                        // -----------------------------------------

                        HttpContext.Session.SetInt32(
                            "CustomerId",
                            customerId);

                        HttpContext.Session.SetInt32(
                            "UserId",
                            userId);

                        HttpContext.Session.SetString(
                            "CustomerName",
                            fullName);

                        HttpContext.Session.SetString(
                            "CustomerEmail",
                            customerEmail);

                        return RedirectToAction(
                            "Dashboard");
                    }
                }

                ModelState.AddModelError(
                    "",
                    "Invalid email or password.");

                return View();
            }
            catch (SqlException)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to connect to the database.");

                return View();
            }
        }


        // =====================================================
        // CUSTOMER DASHBOARD
        // =====================================================

        [HttpGet]
        public IActionResult Dashboard()
        {
            int? customerId =
                HttpContext.Session.GetInt32(
                    "CustomerId");

            if (customerId == null)
            {
                return RedirectToAction("Login");
            }

            Customer customer = new Customer();

            try
            {
                using SqlConnection connection =
                    new SqlConnection(_connectionString);

                connection.Open();

                string query = @"
                    SELECT 
                        u.FullName, 
                        u.Email, 
                        u.Phone, 
                        c.Address, 
                        c.Gender 
                    FROM Customers c
                    INNER JOIN Users u ON c.UserId = u.UserId
                    WHERE c.CustomerId = @CustomerId";

                using SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CustomerId", customerId.Value);

                using SqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    customer.FullName = reader["FullName"].ToString() ?? "";
                    customer.Email = reader["Email"].ToString() ?? "";
                    customer.Phone = reader["Phone"] == DBNull.Value ? "" : reader["Phone"].ToString() ?? "";
                    customer.Address = reader["Address"] == DBNull.Value ? "" : reader["Address"].ToString() ?? "";
                    customer.Gender = reader["Gender"] == DBNull.Value ? "" : reader["Gender"].ToString() ?? "";
                }
            }
            catch (SqlException)
            {
                customer.FullName = HttpContext.Session.GetString("CustomerName") ?? "Customer";
                customer.Email = HttpContext.Session.GetString("CustomerEmail") ?? "";
            }

            return View(customer);
        }


        // =====================================================
        // CUSTOMER PROFILE - GET
        // =====================================================

        [HttpGet]
        public IActionResult Profile()
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerId");

            if (customerId == null)
            {
                return RedirectToAction("Login");
            }

            Customer customer = new Customer();

            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();

                string query = @"
                    SELECT 
                        u.FullName, 
                        u.Email, 
                        u.Phone, 
                        c.Address, 
                        c.Gender 
                    FROM Customers c
                    INNER JOIN Users u ON c.UserId = u.UserId
                    WHERE c.CustomerId = @CustomerId";

                using SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CustomerId", customerId.Value);

                using SqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    customer.FullName = reader["FullName"].ToString() ?? "";
                    customer.Email = reader["Email"].ToString() ?? "";
                    customer.Phone = reader["Phone"] == DBNull.Value ? "" : reader["Phone"].ToString() ?? "";
                    customer.Address = reader["Address"] == DBNull.Value ? "" : reader["Address"].ToString() ?? "";
                    customer.Gender = reader["Gender"] == DBNull.Value ? "" : reader["Gender"].ToString() ?? "";
                }
            }
            catch (SqlException)
            {
                TempData["Error"] = "Unable to load profile details.";
            }

            return View(customer);
        }

        // =====================================================
        // CUSTOMER PROFILE - POST (UPDATE DETAILS)
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(Customer customer)
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerId");
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (customerId == null || userId == null)
            {
                return RedirectToAction("Login");
            }

            // Password field ko profile update ke liye ModelState se hata dete hain taaki blank hone par error na aaye
            ModelState.Remove("Password");

            if (string.IsNullOrWhiteSpace(customer.FullName))
            {
                ModelState.AddModelError("FullName", "Full name is required.");
            }

            if (string.IsNullOrWhiteSpace(customer.Phone))
            {
                ModelState.AddModelError("Phone", "Phone number is required.");
            }

            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();

                // 1. UPDATE USERS TABLE
                string userUpdateQuery = @"
                    UPDATE Users 
                    SET FullName = @FullName, 
                        Phone = @Phone";

                // Sirf tabhi password update karein jab user ne naya password dala ho
                bool updatePassword = !string.IsNullOrWhiteSpace(customer.Password);
                if (updatePassword)
                {
                    userUpdateQuery += ", Password = @Password";
                }

                userUpdateQuery += " WHERE UserId = @UserId";

                using (SqlCommand userCmd = new SqlCommand(userUpdateQuery, connection))
                {
                    userCmd.Parameters.AddWithValue("@FullName", customer.FullName);
                    userCmd.Parameters.AddWithValue("@Phone", customer.Phone);
                    userCmd.Parameters.AddWithValue("@UserId", userId.Value);

                    if (updatePassword)
                    {
                        var passwordHasher = new PasswordHasher<Customer>();
                        string hashedPassword = passwordHasher.HashPassword(customer, customer.Password);
                        userCmd.Parameters.AddWithValue("@Password", hashedPassword);
                    }

                    userCmd.ExecuteNonQuery();
                }

                // 2. UPDATE CUSTOMERS TABLE
                string customerUpdateQuery = @"
                    UPDATE Customers 
                    SET Address = @Address, 
                        Gender = @Gender 
                    WHERE CustomerId = @CustomerId";

                using (SqlCommand custCmd = new SqlCommand(customerUpdateQuery, connection))
                {
                    custCmd.Parameters.AddWithValue("@Address", string.IsNullOrWhiteSpace(customer.Address) ? DBNull.Value : customer.Address);
                    custCmd.Parameters.AddWithValue("@Gender", string.IsNullOrWhiteSpace(customer.Gender) ? DBNull.Value : customer.Gender);
                    custCmd.Parameters.AddWithValue("@CustomerId", customerId.Value);

                    custCmd.ExecuteNonQuery();
                }

                HttpContext.Session.SetString("CustomerName", customer.FullName);

                TempData["Success"] = "Profile updated successfully!";
                return RedirectToAction("Profile");
            }
            catch (SqlException)
            {
                ModelState.AddModelError("", "Unable to update profile. Please try again.");
                return View(customer);
            }
        }


        // =====================================================
        // BOOK APPOINTMENT - GET
        // =====================================================

        [HttpGet]
        public IActionResult BookAppointment()
        {
            int? customerId =
                HttpContext.Session.GetInt32(
                    "CustomerId");

            if (customerId == null)
            {
                return RedirectToAction("Login");
            }


            // ---------------------------------------------
            // LOAD HAIRSTYLES
            // ---------------------------------------------

            ViewBag.Hairstyles =
                GetHairstyles();


            // ---------------------------------------------
            // LOAD ACTIVE BARBERS
            // ---------------------------------------------

            ViewBag.Barbers =
                GetActiveBarbers();


            return View(
                new Appointment
                {
                    AppointmentDate =
                        DateTime.Today.AddDays(1),

                    Status = "Pending"
                });
        }


        // =====================================================
        // BOOK APPOINTMENT - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BookAppointment(
            Appointment appointment)
        {
            int? customerId =
                HttpContext.Session.GetInt32(
                    "CustomerId");

            if (customerId == null)
            {
                return RedirectToAction("Login");
            }


            // ---------------------------------------------
            // DATE VALIDATION
            // ---------------------------------------------

            if (appointment.AppointmentDate.Date <
                DateTime.Today)
            {
                ModelState.AddModelError(
                    "AppointmentDate",
                    "Appointment date cannot be in the past.");
            }


            // ---------------------------------------------
            // TIME VALIDATION
            // ---------------------------------------------

            if (appointment.AppointmentTime <=
                TimeSpan.Zero)
            {
                ModelState.AddModelError(
                    "AppointmentTime",
                    "Please select a valid appointment time.");
            }


            if (!ModelState.IsValid)
            {
                ViewBag.Hairstyles =
                    GetHairstyles();

                ViewBag.Barbers =
                    GetActiveBarbers();

                return View(appointment);
            }


            try
            {
                using SqlConnection connection =
                    new SqlConnection(_connectionString);

                connection.Open();


                // ---------------------------------------------
                // CHECK BARBER
                // ---------------------------------------------

                string barberCheckQuery = @"
                    SELECT COUNT(*)
                    FROM Barbers
                    WHERE BarberId = @BarberId
                    AND Status = 'Active'";

                using SqlCommand barberCheckCommand =
                    new SqlCommand(
                        barberCheckQuery,
                        connection);

                barberCheckCommand.Parameters.AddWithValue(
                    "@BarberId",
                    appointment.BarberId);

                int barberExists =
                    Convert.ToInt32(
                        barberCheckCommand.ExecuteScalar());

                if (barberExists == 0)
                {
                    ModelState.AddModelError(
                        "BarberId",
                        "Selected barber is not available.");

                    ViewBag.Hairstyles =
                        GetHairstyles();

                    ViewBag.Barbers =
                        GetActiveBarbers();

                    return View(appointment);
                }


                // ---------------------------------------------
                // CHECK SERVICE
                // ---------------------------------------------

                string styleCheckQuery = @"
                    SELECT COUNT(*)
                    FROM Hairstyles
                    WHERE StyleId = @StyleId";

                using SqlCommand styleCheckCommand =
                    new SqlCommand(
                        styleCheckQuery,
                        connection);

                styleCheckCommand.Parameters.AddWithValue(
                    "@StyleId",
                    appointment.StyleId);

                int styleExists =
                    Convert.ToInt32(
                        styleCheckCommand.ExecuteScalar());

                if (styleExists == 0)
                {
                    ModelState.AddModelError(
                        "StyleId",
                        "Selected service is not available.");

                    ViewBag.Hairstyles =
                        GetHairstyles();

                    ViewBag.Barbers =
                        GetActiveBarbers();

                    return View(appointment);
                }


                // ---------------------------------------------
                // CHECK SAME BARBER / DATE / TIME
                // ---------------------------------------------

                string bookingCheckQuery = @"
                    SELECT COUNT(*)
                    FROM Appointments
                    WHERE BarberId = @BarberId
                    AND AppointmentDate = @AppointmentDate
                    AND AppointmentTime = @AppointmentTime
                    AND Status IN ('Pending', 'Confirmed')";

                using SqlCommand bookingCheckCommand =
                    new SqlCommand(
                        bookingCheckQuery,
                        connection);

                bookingCheckCommand.Parameters.AddWithValue(
                    "@BarberId",
                    appointment.BarberId);

                bookingCheckCommand.Parameters.AddWithValue(
                    "@AppointmentDate",
                    appointment.AppointmentDate.Date);

                bookingCheckCommand.Parameters.AddWithValue(
                    "@AppointmentTime",
                    appointment.AppointmentTime);

                int alreadyBooked =
                    Convert.ToInt32(
                        bookingCheckCommand.ExecuteScalar());

                if (alreadyBooked > 0)
                {
                    ModelState.AddModelError(
                        "",
                        "This barber is already booked at the selected date and time.");

                    ViewBag.Hairstyles =
                        GetHairstyles();

                    ViewBag.Barbers =
                        GetActiveBarbers();

                    return View(appointment);
                }


                // ---------------------------------------------
                // INSERT APPOINTMENT
                // ---------------------------------------------

                string insertQuery = @"
                    INSERT INTO Appointments
                    (
                        CustomerId,
                        BarberId,
                        StyleId,
                        AppointmentDate,
                        AppointmentTime,
                        Status
                    )
                    VALUES
                    (
                        @CustomerId,
                        @BarberId,
                        @StyleId,
                        @AppointmentDate,
                        @AppointmentTime,
                        'Pending'
                    )";

                using SqlCommand insertCommand =
                    new SqlCommand(
                        insertQuery,
                        connection);

                insertCommand.Parameters.AddWithValue(
                    "@CustomerId",
                    customerId.Value);

                insertCommand.Parameters.AddWithValue(
                    "@BarberId",
                    appointment.BarberId);

                insertCommand.Parameters.AddWithValue(
                    "@StyleId",
                    appointment.StyleId);

                insertCommand.Parameters.AddWithValue(
                    "@AppointmentDate",
                    appointment.AppointmentDate.Date);

                insertCommand.Parameters.AddWithValue(
                    "@AppointmentTime",
                    appointment.AppointmentTime);

                insertCommand.ExecuteNonQuery();


                TempData["Success"] =
                    "Appointment booked successfully! Waiting for confirmation.";

                return RedirectToAction(
                    "Dashboard");
            }
            catch (SqlException)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to book appointment. Please try again.");

                ViewBag.Hairstyles =
                    GetHairstyles();

                ViewBag.Barbers =
                    GetActiveBarbers();

                return View(appointment);
            }
        }


        // =====================================================
        // LOAD HAIRSTYLES
        // =====================================================

        private List<Hairstyle> GetHairstyles()
        {
            List<Hairstyle> hairstyles =
                new List<Hairstyle>();

            using SqlConnection connection =
                new SqlConnection(_connectionString);

            connection.Open();

            string query = @"
                SELECT
                    StyleId,
                    StyleName,
                    Description,
                    Price,
                    Duration,
                    Image
                FROM Hairstyles
                ORDER BY StyleName";

            using SqlCommand command =
                new SqlCommand(query, connection);

            using SqlDataReader reader =
                command.ExecuteReader();

            while (reader.Read())
            {
                hairstyles.Add(
                    new Hairstyle
                    {
                        StyleId =
                            Convert.ToInt32(
                                reader["StyleId"]),

                        StyleName =
                            reader["StyleName"].ToString()
                            ?? "",

                        Description =
                            reader["Description"] == DBNull.Value
                                ? ""
                                : reader["Description"].ToString()
                                  ?? "",

                        Price =
                            reader["Price"] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(
                                    reader["Price"]),

                        Duration =
                            reader["Duration"] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(
                                    reader["Duration"]),

                        Image =
                            reader["Image"] == DBNull.Value
                                ? ""
                                : reader["Image"].ToString()
                                  ?? ""
                    });
            }

            return hairstyles;
        }


        // =====================================================
        // LOAD ACTIVE BARBERS
        // =====================================================

        private List<Barber> GetActiveBarbers()
        {
            List<Barber> barbers =
                new List<Barber>();

            using SqlConnection connection =
                new SqlConnection(_connectionString);

            connection.Open();

            string query = @"
                SELECT
                    BarberId,
                    UserId,
                    Experience,
                    Specialization,
                    Status
                FROM Barbers
                WHERE Status = 'Active'
                ORDER BY Experience DESC";

            using SqlCommand command =
                new SqlCommand(query, connection);

            using SqlDataReader reader =
                command.ExecuteReader();

            while (reader.Read())
            {
                barbers.Add(
                    new Barber
                    {
                        BarberId =
                            Convert.ToInt32(
                                reader["BarberId"]),

                        UserId =
                            Convert.ToInt32(
                                reader["UserId"]),

                        Experience =
                            reader["Experience"] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(
                                    reader["Experience"]),

                        Specialization =
                            reader["Specialization"] == DBNull.Value
                                ? ""
                                : reader["Specialization"].ToString()
                                  ?? "",

                        Status =
                            reader["Status"] == DBNull.Value
                                ? ""
                                : reader["Status"].ToString()
                                  ?? ""
                    });
            }

            return barbers;
        }


        // =====================================================
        // MY APPOINTMENTS
        // =====================================================

        [HttpGet]
        public IActionResult MyAppointments()
        {
            int? customerId =
                HttpContext.Session.GetInt32("CustomerId");

            if (customerId == null)
            {
                return RedirectToAction("Login");
            }

            List<Appointment> appointments =
                new List<Appointment>();

            try
            {
                using SqlConnection connection =
                    new SqlConnection(_connectionString);

                connection.Open();

                string query = @"
            SELECT
                a.AppointmentId,
                a.CustomerId,
                a.BarberId,
                a.StyleId,
                a.AppointmentDate,
                a.AppointmentTime,
                a.Status,

                h.StyleName,
                h.Price,
                h.Duration,

                b.Specialization,
                b.Experience,

                u.FullName AS BarberName

            FROM Appointments a

            INNER JOIN Hairstyles h
                ON a.StyleId = h.StyleId

            INNER JOIN Barbers b
                ON a.BarberId = b.BarberId

            INNER JOIN Users u
                ON b.UserId = u.UserId

            WHERE a.CustomerId = @CustomerId

            ORDER BY
                a.AppointmentDate DESC,
                a.AppointmentTime DESC";

                using SqlCommand command =
                    new SqlCommand(query, connection);

                command.Parameters.AddWithValue(
                    "@CustomerId",
                    customerId.Value);

                using SqlDataReader reader =
                    command.ExecuteReader();

                while (reader.Read())
                {
                    appointments.Add(
                        new Appointment
                        {
                            AppointmentId =
                                Convert.ToInt32(
                                    reader["AppointmentId"]),

                            CustomerId =
                                Convert.ToInt32(
                                    reader["CustomerId"]),

                            BarberId =
                                Convert.ToInt32(
                                    reader["BarberId"]),

                            StyleId =
                                Convert.ToInt32(
                                    reader["StyleId"]),

                            AppointmentDate =
                                Convert.ToDateTime(
                                    reader["AppointmentDate"]),

                            AppointmentTime =
                                (TimeSpan)
                                    reader["AppointmentTime"],

                            Status =
                                reader["Status"].ToString()
                                ?? "Pending",

                            StyleName =
                                reader["StyleName"].ToString()
                                ?? "",

                            BarberName =
                                reader["BarberName"].ToString()
                                ?? "",

                            Price =
                                reader["Price"] == DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(
                                        reader["Price"]),

                            Duration =
                                reader["Duration"] == DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(
                                        reader["Duration"]),

                            Specialization =
                                reader["Specialization"] == DBNull.Value
                                    ? ""
                                    : reader["Specialization"].ToString()
                                      ?? "",

                            Experience =
                                reader["Experience"] == DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(
                                        reader["Experience"])
                        });
                }
            }
            catch (SqlException)
            {
                ViewBag.Error =
                    "Unable to load appointments. Please try again.";
            }

            ViewBag.Appointments = appointments;

            return View();
        }


        // =====================================================
        // CANCEL APPOINTMENT
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelAppointment(int id)
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerId");

            if (customerId == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();

                string updateQuery = @"
                    UPDATE Appointments
                    SET Status = 'Cancelled'
                    WHERE AppointmentId = @AppointmentId 
                    AND CustomerId = @CustomerId 
                    AND Status = 'Pending'";

                using SqlCommand command = new SqlCommand(updateQuery, connection);
                command.Parameters.AddWithValue("@AppointmentId", id);
                command.Parameters.AddWithValue("@CustomerId", customerId.Value);

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    TempData["Success"] = "Appointment cancelled successfully.";
                }
                else
                {
                    TempData["Error"] = "Unable to cancel appointment.";
                }
            }
            catch (SqlException)
            {
                TempData["Error"] = "Database error occurred while cancelling appointment.";
            }

            return RedirectToAction("MyAppointments");
        }


        // =====================================================
        // CUSTOMER LOGOUT
        // =====================================================

        [HttpGet] // Yeh ensure karega ki GET request allow ho
        public IActionResult Logout()
        {
            // Session clear kar rahe hain
            HttpContext.Session.Clear();

            // Logout ke baad seedha Home page par bhej do
            return RedirectToAction("Index", "Home");
        }
    }
}   