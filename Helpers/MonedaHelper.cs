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
                Moneda.Euros => "€",
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