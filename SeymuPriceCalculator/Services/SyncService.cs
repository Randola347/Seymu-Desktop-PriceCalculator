using SeymuPriceCalculator.Database;
using SeymuPriceCalculator.Models;
using System;
using System.Threading.Tasks;

namespace SeymuPriceCalculator.Services
{
    public static class SyncService
    {
        public static async Task<SyncResult> SincronizarAsync()
        {
            var resultado = new SyncResult();

            try
            {
                bool hayInternet = await NeonService.TestConexionAsync();
                if (!hayInternet)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "Sin conexión a Neon";
                    return resultado;
                }

                await NeonService.InicializarTablasAsync();

                int cotizacionesSubidas = await NeonService.SubirCotizacionesAsync();
                await NeonService.SubirPiezasAsync();
                await NeonService.SubirClientesAsync();
                await NeonService.SubirCatalogosAsync();

                resultado.Subidos = cotizacionesSubidas;

                int cotizacionesDescargadas = await NeonService.DescargarCotizacionesAsync();
                await NeonService.DescargarClientesAsync();
                await NeonService.DescargarEmpresaAsync();

                resultado.Descargados = cotizacionesDescargadas;
                resultado.Exitoso = true;
                resultado.Mensaje = "OK";
            }
            catch (Exception ex)
            {
                resultado.Exitoso = false;
                resultado.Mensaje = ex.Message;
            }

            return resultado;
        }
    }
}