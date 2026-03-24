using System.Net;
using System.Net.Mail;
using System.Net.Mime; // NUEVO: Necesario para las imágenes adjuntas

public class EmailService
{
    public async Task EnviarCorreo(string destino, string asunto, string mensaje, byte[] imagenBytes = null)
    {
        try
        {
            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                UseDefaultCredentials = false, // Muy importante: primero esto en false
                Credentials = new NetworkCredential("mispresupuestos.app@gmail.com", "rhun zjmj egss zccx")
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("mispresupuestos.app@gmail.com", "App Presupuesto Familiar"),
                Subject = asunto,
                Body = mensaje,
                IsBodyHtml = true
            };
            mailMessage.To.Add(destino);
            // Si hay una imagen, la preparamos como "Recurso Vinculado" (CID)
            if (imagenBytes != null && imagenBytes.Length > 0)
            {
                AlternateView htmlView = AlternateView.CreateAlternateViewFromString(mensaje, null, MediaTypeNames.Text.Html);

                LinkedResource imagenVinculada = new LinkedResource(new MemoryStream(imagenBytes), "image/png");
                imagenVinculada.ContentId = "graficoID"; // Este es el "puente" entre la imagen y el HTML

                htmlView.LinkedResources.Add(imagenVinculada);
                mailMessage.AlternateViews.Add(htmlView);
            }
            else
            {
                // Si es un correo normal (como recuperar contraseña), solo enviamos el texto
                mailMessage.Body = mensaje;
            }

            await client.SendMailAsync(mailMessage);
        }
        catch (Exception ex)
        {
            // Esto te permite ver el error real en la consola de Visual Studio
            Console.WriteLine("ERROR SMTP: " + ex.Message);
            throw;
        }
    }
}