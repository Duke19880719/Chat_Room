# 即時聊天室系統（Vue 3 + ASP.NET Core + SignalR）

這是一個使用 Vue 3 + TypeScript 與 ASP.NET Core SignalR 所開發的即時聊天室系統。

專案結合了：

- SignalR 即時雙向通訊
- Vue 3 Composition API
- TypeScript
- ASP.NET Core Web API
- 群組聊天室（Room）
- CORS 跨網域
- 自動重連機制
- 賽博龐克 × 和風 UI 特效

---

# 功能特色

## 即時聊天

- 大廳全域聊天
- 房間聊天室
- 即時同步訊息
- 多人聊天室

---

## SignalR 功能

- WebSocket 即時通訊
- Hub 雙向連線
- Group 群組聊天室
- 自動重連
- 即時廣播

---

## UI 特效

- Cyberpunk 風格
- 和風道場 UI
- Ripple 波紋動畫
- 雷射霓虹光效
- 掃描線 CRT 效果
- Canvas 動畫背景

---

# 專案架構

```text
Chat_Room/
│
├── Chat_Room.Server
│   ├── Service_Class
│   │   └── ChatHub.cs
│   ├── Program.cs
│   └── ...
│
├── Chat_Room.Client
│   ├── components
│   │   └── ChatRoom.vue
│   ├── App.vue
│   └── ...
```

---

# SignalR 技術介紹

## SignalR 是什麼？

SignalR 是 ASP.NET Core 提供的即時通訊框架。

它可以做到：

- Server 主動推送資料給 Client
- Client 與 Server 雙向通訊
- 不需要重新整理頁面

類似：

- Discord
- LINE
- Messenger
- Slack

---

## SignalR 運作原理

傳統 HTTP：

```text
Client ---> Request ---> Server
```

HTTP 是：

- Request / Response
- 用戶不發 request，server 無法主動推送

但聊天室需要：

```text
Server ---> 主動推送訊息 ---> 所有人
```

因此 SignalR 會自動選擇：

- WebSocket（優先）
- Server Sent Events
- Long Polling

來實現即時通訊。

---

# SignalR 核心概念

---

## Hub

Hub 是 SignalR 的核心。

用途：

- 接收前端呼叫
- 發送訊息給 Client
- 管理聊天室

本專案：

```csharp
public class ChatHub : Hub<IChatClient>
```

---

## Client

Client 可以是：

- Vue
- React
- Angular
- Mobile App

本專案使用：

```ts
@microsoft/signalr
```

建立與後端的即時連線。

---

## ConnectionId

每個連線都會有唯一 ID：

```text
7f3c0c29-xxxx-xxxx
```

用途：

- 識別使用者
- 私訊
- 房間管理

---

## Group（聊天室）

SignalR 的 Group：

```text
Group = 聊天室
```

可以：

- 把多人加入同一群組
- 對群組廣播

例如：

```csharp
await Groups.AddToGroupAsync(Context.ConnectionId, room);
```

---

# 後端程式碼解說

---

# ChatHub.cs

```csharp
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

        // 加入指定的聊天室
        public async Task join_room(string user, string room)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, room);

            await Clients.Group(room)
                .ReceiveMessage("系統",
                $"{user} 加入了房間 {room}",
                room);
        }

        // 對指定聊天室發送訊息
        public async Task send_room_message(
            string user,
            string message,
            string room)
        {
            await Clients.Group(room)
                .ReceiveMessage(user, message, room);
        }
    }
}
```

---

## send_global_message()

```csharp
public async Task send_global_message(string user, string message)
{
    await Clients.All.ReceiveMessage(user, message, "大廳");
}
```

用途：

- 發送全域聊天室訊息
- 所有人都會收到

---

## join_room()

```csharp
public async Task join_room(string user, string room)
{
    await Groups.AddToGroupAsync(Context.ConnectionId, room);

    await Clients.Group(room)
        .ReceiveMessage("系統",
        $"{user} 加入了房間 {room}",
        room);
}
```

用途：

- 加入指定聊天室
- 通知聊天室所有成員

---

## send_room_message()

```csharp
public async Task send_room_message(
    string user,
    string message,
    string room)
{
    await Clients.Group(room)
        .ReceiveMessage(user, message, room);
}
```

用途：

- 對指定聊天室發送訊息
- 只有該房間的人會收到

---

# Program.cs 解說

---

## 註冊 SignalR

```csharp
builder.Services.AddSignalR();
```

啟用 SignalR 功能。

---

## 註冊 CORS

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:52020")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

---

## 為什麼需要 CORS？

因為：

```text
Vue Frontend:
https://localhost:52020

ASP.NET Backend:
https://localhost:7051
```

屬於不同 Port。

瀏覽器會阻擋跨網域請求，因此必須開啟 CORS。

---

## 啟用 CORS Middleware

```csharp
app.UseCors();
```

必須放在：

```csharp
app.UseAuthorization();
```

之前。

---

## 註冊 Hub 路由

```csharp
app.MapHub<ChatHub>("/hubs/chat");
```

代表：

```text
https://localhost:7051/hubs/chat
```

是 SignalR Hub 的連線位置。

---

# 前端程式碼解說（Vue 3）

---

# 建立 SignalR 連線

```ts
connection = new HubConnectionBuilder()
  .withUrl('https://localhost:7051/hubs/chat')
  .configureLogging(LogLevel.Information)
  .withAutomaticReconnect()
  .build();
```

---

## withUrl()

指定 Hub URL。

---

## configureLogging()

開啟 SignalR log。

---

## withAutomaticReconnect()

自動重連。

避免：

- 網路中斷
- Server 重啟
- WiFi 斷線

導致聊天室失效。

---

# 接收訊息

```ts
connection.on(
  'ReceiveMessage',
  (user, message, room) => {
    messages.value.push({
      user,
      message,
      room
    });
  }
);
```

用途：

- 接收後端推送訊息
- 更新聊天室畫面

---

# 啟動連線

```ts
await connection.start();
```

建立：

```text
Vue <-> SignalR Hub
```

即時通訊連線。

---

# 加入聊天室

```ts
await connection.invoke(
  'join_room',
  username.value,
  roomName.value
);
```

用途：

- 呼叫後端 join_room()
- 加入指定聊天室

---

# 發送大廳訊息

```ts
await connection.invoke(
  'send_global_message',
  username.value,
  inputMessage.value
);
```

---

# 發送房間訊息

```ts
await connection.invoke(
  'send_room_message',
  username.value,
  inputMessage.value,
  currentRoom.value
);
```

---

# Vue Composition API 解說

---

## ref()

```ts
const username = ref('');
```

建立響應式資料。

---

## onMounted()

```ts
onMounted(() => {
  initRipple();
});
```

元件載入後執行。

---

## onUnmounted()

```ts
onUnmounted(async () => {
  if (connection) await connection.stop();
});
```

離開頁面時：

- 關閉 SignalR
- 釋放資源
- 避免 Memory Leak

---

# UI 特效系統

---

# Ripple 波紋特效

使用：

```html
<canvas>
```

搭配：

```ts
requestAnimationFrame(draw);
```

產生高效能動畫。

---

# Grid 背景

```css
background-image:
linear-gradient(...)
```

模擬：

```text
數位塌塌米
```

效果。

---

# 掃描線特效

```css
.scanline-overlay
```

模擬：

- CRT
- Cyberpunk HUD

---

# 自動捲動到底部

```ts
msgScroll.value.scrollTop =
  msgScroll.value.scrollHeight;
```

聊天室收到訊息後：

- 自動滑到底部

---

# 聊天流程圖

```text
使用者輸入訊息
        ↓
Vue invoke()
        ↓
SignalR Hub
        ↓
Clients.All / Group
        ↓
所有 Client 收到
        ↓
Vue 更新 messages
        ↓
畫面即時刷新
```

---

# SignalR 的優點

## 1. 即時同步

不需 refresh。

---

## 2. 雙向通訊

Server 可主動推送。

---

## 3. 自動重連

提高穩定性。

---

## 4. 支援群組

非常適合聊天室。

---

## 5. 跨平台

支援：

- Web
- Mobile
- Desktop

---


# 技術總結

| 技術 | 用途 |
|---|---|
| SignalR | 即時通訊 |
| Hub | 聊天中心 |
| Group | 聊天室 |
| WebSocket | 即時雙向連線 |
| Vue 3 | 前端框架 |
| Composition API | 狀態管理 |
| TypeScript | 型別安全 |
| Canvas | 動畫特效 |
| CORS | 跨網域處理 |

---

# 執行方式

## 啟動 ASP.NET Core

```bash
dotnet run
```

---

## 啟動 Vue

```bash
npm install
npm run dev
```

---

# 測試方式

1. 開啟兩個瀏覽器
2. 使用不同名稱登入
3. 傳送訊息
4. 測試房間功能
5. 測試即時同步

---

本專案為：

- Vue 3
- ASP.NET Core
- SignalR

即時聊天室實作練習。

整合：

- 聊天室
- 群組聊天
- 即時同步
- Cyberpunk UI 設計
- 和風動畫特效