using Microsoft.AspNetCore.SignalR;

namespace IT_Destek_Panel.Hubs
{
    public class TicketHub : Hub
    {
        // Kullanıcıyı biletin "odasına" bağlayalım ki sadece o biletin mesajlarını alsın
        public async Task JoinTicketGroup(int ticketId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, ticketId.ToString());
        }
    }
}