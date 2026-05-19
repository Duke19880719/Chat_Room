using Microsoft.AspNetCore.SignalR;
using Chat_Room.Server.Interface;

namespace Chat_Room.Server.Service_Class
{
    public class ChatHub : Hub<IChatClient>
    {
        // 發送廣播消息給所有連接的客戶端
        public async Task send_global_message(string user, string message)
        {
            await Clients.All.ReceiveMessage(user, message, "大廳");
        }

        // 加入指定的聊天室，並通知該聊天室的成員
        public async Task join_room(string user, string room)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, room);
            await Clients.Group(room).ReceiveMessage("系統", $"{user} 加入了房間  {room}", room);
        }

        //  對特定聊天室發送訊息
        public async Task send_room_message(string user, string message, string room)
        {
            await Clients.Group(room).ReceiveMessage(user, message, room);
        }

    }
}
