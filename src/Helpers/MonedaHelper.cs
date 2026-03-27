using PresupuestoFamiliarApp.Models;
namespace PresupuestoFamiliarApp.Helpers
{
    public static class MonedaHelper
    {
        public static string ObtenerSimbolo(Moneda moneda)
        {
            return moneda switch
            {
                Moneda.Dolares => "$",
                Moneda.Euros => "\u20AC",   // € como escape Unicode → independiente del encoding del archivo
                _ => "S/"
            };
        }

        public static string FormatearMonto(decimal monto, Moneda moneda)
        {
            string simbolo = ObtenerSimbolo(moneda);
            return $"{simbolo} {monto:N2}";
        }
    }
}