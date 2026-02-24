using PresupuestoFamiliarApp.Models; // Asegúrate de que el namespace sea correcto

namespace PresupuestoFamiliarApp.ViewModels
{
    public class DashboardViewModel
    {
        // Añade estas dos nuevas propiedades:
        public int MesSeleccionado { get; set; }
        public int AnioSeleccionado { get; set; }

        // AÑADE ESTA NUEVA PROPIEDAD:
        public string SimboloMonedaEspacio { get; set; }

        // Lo que ya tenías sigue igual:
        public decimal TotalIngresosMes { get; set; }
        public decimal TotalEgresosMes { get; set; }
        public decimal AhorroMes => TotalIngresosMes - TotalEgresosMes;

        // REEMPLAZA LA LISTA DE CUENTAS POR ESTAS DOS:
        public List<Cuenta> CuentasDinero { get; set; } = new();
        public List<Cuenta> TarjetasCredito { get; set; } = new();

        // Suma total de lo que debes pagar
        public decimal DeudaTotalTarjetas => TarjetasCredito.Sum(t => t.SaldoActual);
        public List<CategoriaResumen> ResumenCategorias { get; set; } = new();
        // AÑADE ESTA NUEVA LÍNEA:
        public List<BalanceMensual> BalancesHistoricos { get; set; } = new();
    }

    // TUS CLASES DE APOYO (Al final del archivo):
    

    // AÑADE ESTA NUEVA CLASE:
    public class BalanceMensual
    {
        public string MesNombre { get; set; }
        public decimal Balance { get; set; }
    }
    public class CategoriaResumen
    {
        public string Nombre { get; set; }
        public decimal PresupuestoMensual { get; set; }
        public decimal GastoReal { get; set; }

        // Calculamos el porcentaje consumido para la barra de progreso
        public int PorcentajeConsumido => PresupuestoMensual == 0 ? 0 :
            (int)Math.Min((GastoReal / PresupuestoMensual) * 100, 100);
    }
}