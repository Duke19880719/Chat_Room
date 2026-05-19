namespace Chat_Room.Server.Interface
{
    public interface IChatClient
    {
        Task ReceiveMessage(string user, string message, string room);
    }
}
