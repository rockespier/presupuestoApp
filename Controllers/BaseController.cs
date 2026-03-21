using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PresupuestoFamiliarApp.Data;
using PresupuestoFamiliarApp.Helpers;

namespace PresupuestoFamiliarApp.Controllers
{
    public class BaseController : Controller
    {
        protected readonly PresupuestoContext _context;

        public BaseController(PresupuestoContext context)
        {
            _context = context;
        }

        protected int ObtenerEspacioActivoId()
        {
            return int.TryParse(Request.Cookies["EspacioActivoId"], out int id) ? id : 1;
        }

        // Este método se ejecuta ANTES de cada acción del controlador
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            int espacioId = ObtenerEspacioActivoId();
            var espacio = await _context.Espacios.FindAsync(espacioId);
            
            if (espacio != null)
            {
                ViewBag.SimboloMoneda = MonedaHelper.ObtenerSimbolo(espacio.MonedaPrincipal);
                ViewBag.MonedaEspacio = espacio.MonedaPrincipal;
            }
            else
            {
                ViewBag.SimboloMoneda = "S/";
            }

            await base.OnActionExecutionAsync(context, next);
        }
    }
}