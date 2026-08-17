using Microsoft.AspNetCore.SignalR;

namespace ErzurumFlight.Server.Hubs;

/// <summary>
/// SignalR hub'ı: /hubs/flights. Kullanıcı sayfayı yenilemeden canlı uçuş durumu ve konum
/// güncellemelerini alır. Sunucudan istemciye gönderilen olaylar:
/// FlightStatusChanged, FlightCancelled, FlightPositionUpdated, ScheduleSynced.
/// İstemci bu hub üzerinde metod çağırmaz; yalnızca dinler (server-to-client push).
/// </summary>
public class FlightHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // Tüm bağlı istemciler tek bir genel gruba katılır; ilk sürümde havalimanı tek olduğu için
        // ayrı gruplama gerekmez (ileride çoklu havalimanı eklenirse havalimanı bazlı gruplara geçilebilir).
        await Groups.AddToGroupAsync(Context.ConnectionId, "erzurum");
        await base.OnConnectedAsync();
    }
}
