
using Microsoft.Data.SqlClient;

public class NotificationServiceLegacy
{
    public void SendNotification(string userId, string type, string message)
    {


        if (type == "email")
        {
            SqlConnection conn = new SqlConnection("Server=myserver;Database=notifications;User Id=admin;Password=secret123;");
            conn.Open();
            SqlCommand userCmd = new SqlCommand("SELECT * FROM users WHERE id = '" + userId + "'", conn);
            SqlDataReader reader = userCmd.ExecuteReader();

            if (!reader.Read())
            {
                conn.Close();
                return;
            }

            string email = reader["email"].ToString();
            string name = reader["name"].ToString();
            reader.Close();

            Console.WriteLine("Sending email to " + email);
            Console.WriteLine("Dear " + name + ", " + message);

            SqlCommand logCmd = new SqlCommand("INSERT INTO notification_logs (user_id, type, message, sent_at) VALUES ('" + userId + "', 'email', '" + message + "', '" + DateTime.Now + "')", conn);
            logCmd.ExecuteNonQuery();
            conn.Close();
        }
        else if (type == "sms")
        {
            SqlConnection conn = new SqlConnection("Server=myserver;Database=notifications;User Id=admin;Password=secret123;");
            conn.Open();
            SqlCommand userCmd = new SqlCommand("SELECT * FROM users WHERE id = '" + userId + "'", conn);
            SqlDataReader reader = userCmd.ExecuteReader();

            if (!reader.Read())
            {
                conn.Close();
                return;
            }

            string phone = reader["phone"].ToString();
            string name = reader["name"].ToString();
            reader.Close();

            Console.WriteLine("Sending SMS to " + phone);
            Console.WriteLine("Hi " + name + "! " + message);

            SqlCommand logCmd = new SqlCommand("INSERT INTO notification_logs (user_id, type, message, sent_at) VALUES ('" + userId + "', 'sms', '" + message + "', '" + DateTime.Now + "')", conn);
            logCmd.ExecuteNonQuery();
            conn.Close();
        }
        else if (type == "push")
        {
            SqlConnection conn = new SqlConnection("Server=myserver;Database=notifications;User Id=admin;Password=secret123;");
            conn.Open();
            SqlCommand userCmd = new SqlCommand("SELECT * FROM users WHERE id = '" + userId + "'", conn);
            SqlDataReader reader = userCmd.ExecuteReader();

            if (!reader.Read())
            {
                conn.Close();
                return;
            }

            string deviceToken = reader["device_token"].ToString();
            reader.Close();

            Console.WriteLine("Sending push notification to device " + deviceToken);
            Console.WriteLine(message);

            SqlCommand logCmd = new SqlCommand("INSERT INTO notification_logs (user_id, type, message, sent_at) VALUES ('" + userId + "', 'push', '" + message + "', '" + DateTime.Now + "')", conn);
            logCmd.ExecuteNonQuery();
            conn.Close();
        }
    }
}