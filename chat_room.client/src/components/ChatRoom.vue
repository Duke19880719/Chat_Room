<template>
  <div class="dojo-shell">
    <!-- 波紋背景特效 -->
    <canvas class="ripple-canvas" ref="rippleCanvas"></canvas>

    <!-- 裝飾性格子紋（和風疊蓆雷射網格） -->
    <div class="tatami-grid"></div>

    <!-- 頂部掃描線與噪點投影 -->
    <div class="scanline-overlay"></div>

    <!-- 主要容器 -->
    <div class="dojo-container">

      <!-- 頂部 LOGO 橫幅（極光漸層） -->
      <header class="dojo-header">
        <div class="header-kamon">⬢</div>
        <div class="header-title">
          <span class="title-jp">通信道場</span>
          <span class="title-sub">SIGNAL · DOJO · REALTIME · SYSTEM</span>
        </div>
        <div class="header-kamon header-kamon--right">⬢</div>
      </header>

      <!-- 霓虹漸層分隔線 -->
      <div class="neon-divider">
        <span class="divider-diamond">◆</span>
      </div>

      <!-- 登入介面 -->
      <div class="login-scroll" v-if="!isJoined">
        <div class="scroll-content">
          <div class="scroll-label">入 場 登 錄</div>

          <div class="field-group">
            <label class="field-label">武士名号 </label>
            <div class="input-wrap">
              <span class="input-glyph">侍</span>
              <input v-model="username"
                     placeholder="輸入你的暱稱..."
                     class="cyber-input"
                     @keyup.enter="username && startChat()" />
            </div>
          </div>

          <div class="field-group">
            <label class="field-label">道場房間</label>
            <div class="input-wrap">
              <span class="input-glyph">間</span>
              <input v-model="roomName"
                     placeholder="進入指定房間（選填）"
                     class="cyber-input" />
            </div>
          </div>

          <button class="enter-btn"
                  @click="startChat"
                  :disabled="!username">
            <span class="btn-kanji">入</span>
            <span class="btn-text">進入網路道場</span>
            <span class="btn-kanji">道</span>
          </button>
        </div>
      </div>

      <!-- 聊天主介面 -->
      <div v-else class="chat-arena">

        <!-- 側邊欄：用戶資訊 -->
        <aside class="dojo-sidebar">
          <div class="sidebar-avatar">
            <div class="avatar-ring">
              <span class="avatar-char">{{ username.charAt(0).toUpperCase() }}</span>
            </div>
            <div class="avatar-glow"></div>
          </div>
          <div class="sidebar-name">{{ username }}</div>
          <div class="sidebar-room">
            <span class="room-icon">⛩</span>
            <span>{{ currentRoom }}</span>
          </div>
          <div class="sidebar-divider"></div>
          <div class="sidebar-status">
            <span class="status-dot"></span>
            <span class="status-text">ONLINE</span>
          </div>
        </aside>

        <!-- 訊息主區 -->
        <main class="message-dojo">
          <div class="messages-scroll" ref="msgScroll">
            <div v-for="(msg, index) in messages"
                 :key="index"
                 class="msg-row"
                 :class="{
                'msg-row--mine': msg.user === username,
                'msg-row--sys': msg.user === '系統通知'
              }">
              <!-- 系統訊息 -->
              <template v-if="msg.user === '系統通知'">
                <div class="sys-banner">
                  <span class="sys-line"></span>
                  <span class="sys-text">【 LOG 】{{ msg.message }}</span>
                  <span class="sys-line"></span>
                </div>
              </template>

              <!-- 一般訊息 -->
              <template v-else>
                <div class="msg-avatar-wrap">
                  <div class="msg-avatar" :class="{ 'msg-avatar--mine': msg.user === username }">
                    {{ msg.user.charAt(0).toUpperCase() }}
                  </div>
                </div>
                <div class="msg-bubble-wrap">
                  <div class="bubble-meta">
                    <span class="bubble-user">{{ msg.user }}</span>
                    <span class="bubble-room">#{{ msg.room }}</span>
                  </div>
                  <div class="msg-bubble" :class="{ 'msg-bubble--mine': msg.user === username }">
                    <div class="bubble-corner bubble-corner--tl"></div>
                    <div class="bubble-corner bubble-corner--br"></div>
                    {{ msg.message }}
                  </div>
                </div>
              </template>
            </div>
          </div>

          <!-- 輸入列 -->
          <div class="input-dojo">
            <div class="input-blade">
              <span class="blade-tip">//</span>
              <input v-model="inputMessage"
                     @keyup.enter="sendMessage"
                     placeholder="輸入訊號，按 Enter 傳送..."
                     class="blade-input" />
            </div>
            <button class="send-btn" @click="sendMessage">
              <span class="send-kanji">送</span>
            </button>
          </div>
        </main>

      </div>

    </div>
  </div>
</template>

<script setup lang="ts">
  import { ref, onUnmounted, onMounted, nextTick } from 'vue';
  import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

  const username = ref('');
  const roomName = ref('');
  const currentRoom = ref('大廳');
  const inputMessage = ref('');
  const isJoined = ref(false);
  const messages = ref<Array<{ user: string; message: string; room: string }>>([]);
  const rippleCanvas = ref<HTMLCanvasElement | null>(null);
  const msgScroll = ref<HTMLDivElement | null>(null);

  let connection: HubConnection | null = null;
  let animFrame: number;

  // ── 波紋特效 ──────────────────────────────────────────
  const ripples: { x: number; y: number; r: number; alpha: number; color: string }[] = [];
  const palette = ['#ff0055', '#00ffcc', '#ffb703', '#9d4edd'];

  function initRipple() {
    const canvas = rippleCanvas.value;
    if (!canvas) return;
    const ctx = canvas.getContext('2d')!;

    const resize = () => {
      canvas.width = window.innerWidth;
      canvas.height = window.innerHeight;
    };
    resize();
    window.addEventListener('resize', resize);

    const spawnRipple = () => {
      ripples.push({
        x: Math.random() * canvas.width,
        y: Math.random() * canvas.height,
        r: 0,
        alpha: 0.4,
        color: palette[Math.floor(Math.random() * palette.length)],
      });
    };
    const rippleTimer = setInterval(spawnRipple, 800);

    const draw = () => {
      ctx.clearRect(0, 0, canvas.width, canvas.height);
      for (let i = ripples.length - 1; i >= 0; i--) {
        const rp = ripples[i];
        ctx.beginPath();
        ctx.arc(rp.x, rp.y, rp.r, 0, Math.PI * 2);
        ctx.strokeStyle = rp.color;
        ctx.globalAlpha = rp.alpha;
        ctx.lineWidth = 2.0;
        ctx.stroke();

        if (rp.r > 25) {
          ctx.beginPath();
          ctx.arc(rp.x, rp.y, rp.r * 0.65, 0, Math.PI * 2);
          ctx.globalAlpha = rp.alpha * 0.4;
          ctx.stroke();
        }

        rp.r += 1.5;
        rp.alpha -= 0.004;
        if (rp.alpha <= 0) ripples.splice(i, 1);
      }
      ctx.globalAlpha = 1;
      animFrame = requestAnimationFrame(draw);
    };
    draw();

    onUnmounted(() => {
      clearInterval(rippleTimer);
      cancelAnimationFrame(animFrame);
      window.removeEventListener('resize', resize);
    });
  }

  onMounted(() => {
    initRipple();
  });

  // ── SignalR 邏輯（保持不變） ──────────────────────────────
  const startChat = async () => {
    connection = new HubConnectionBuilder()
      .withUrl('https://localhost:7051/hubs/chat')
      .configureLogging(LogLevel.Information)
      .withAutomaticReconnect()
      .build();

    connection.on('ReceiveMessage', (user: string, message: string, room: string) => {
      messages.value.push({ user, message, room });
      nextTick(() => {
        if (msgScroll.value) {
          msgScroll.value.scrollTop = msgScroll.value.scrollHeight;
        }
      });
    });

    try {
      await connection.start();
      isJoined.value = true;

      if (roomName.value.trim()) {
        currentRoom.value = roomName.value;
        await connection.invoke('join_room', username.value, roomName.value);
      }
    } catch (err) {
      alert('連線失敗，請檢查後端服務是否啟動！');
      console.error(err);
    }
  };

  const sendMessage = async () => {
    if (!inputMessage.value.trim() || !connection) return;
    try {
      if (currentRoom.value === '大廳') {
        await connection.invoke('send_global_message', username.value, inputMessage.value);
      } else {
        await connection.invoke('send_room_message', username.value, inputMessage.value, currentRoom.value);
      }
      inputMessage.value = '';
    } catch (err) {
      console.error('訊息傳送失敗:', err);
    }
  };

  onUnmounted(async () => {
    if (connection) await connection.stop();
  });
</script>

<style scoped>
  
  @import url('https://fonts.googleapis.com/css2?family=Noto+Serif+JP:wght@700;900&family=Share+Tech+Mono&display=swap');

  :root {
    --font-jp: 'Noto Serif JP', serif;
    --font-mono: 'Share Tech Mono', monospace;
  }

  /* 全頁背景  */
  .dojo-shell {
    position: fixed;
    inset: 0;
    background: #020205;
    overflow: hidden;
    display: flex;
    align-items: center;
    justify-content: center;
    font-family: var(--font-jp);
    font-weight: 700;
  }

  .ripple-canvas {
    position: absolute;
    inset: 0;
    pointer-events: none;
    z-index: 0;
  }

  .tatami-grid {
    position: absolute;
    inset: 0;
    z-index: 1;
    background-image: linear-gradient(rgba(255, 0, 85, 0.1) 1px, transparent 1px), linear-gradient(90deg, rgba(255, 0, 85, 0.1) 1px, transparent 1px);
    background-size: 5vw 5vw;
  }

  .scanline-overlay {
    position: absolute;
    inset: 0;
    z-index: 2;
    background: linear-gradient(rgba(18, 16, 16, 0) 50%, rgba(0, 0, 0, 0.3) 50%);
    background-size: 100% 0.5vh;
    pointer-events: none;
  }

  /*  彈性響應式主容器 */
  .dojo-container {
    position: relative;
    z-index: 10;
    width: 90vw;
    height: 85vh;
    display: flex;
    flex-direction: column;
    background: rgba(4, 4, 8, 0.92);
    backdrop-filter: blur(20px);
    border: 0.2vw solid #ff0055;
    border-radius: 0.4vw;
    overflow: hidden;
    animation: ShellPulseGlow 4s ease-in-out infinite;
  }

  @keyframes ShellPulseGlow {
    0%, 100% {
      box-shadow: 0 0 2vw rgba(255, 0, 85, 0.4), 0 0 4vw rgba(154, 78, 221, 0.2), inset 0 0 2vw rgba(0, 255, 204, 0.15);
      border-color: #ff0055;
    }

    50% {
      box-shadow: 0 0 4vw rgba(255, 0, 85, 0.7), 0 0 7vw rgba(0, 255, 204, 0.5), inset 0 0 3vw rgba(255, 0, 85, 0.3);
      border-color: #00ffcc;
    }
  }

  /* 傳統和風角飾*/
  .dojo-container::before,
  .dojo-container::after {
    content: '';
    position: absolute;
    width: 3vw;
    height: 3vw;
    border-color: #ff0055;
    border-style: solid;
    z-index: 20;
    pointer-events: none;
    filter: drop-shadow(0 0 0.8vw #ff0055);
  }

  .dojo-container::before {
    top: -0.2vw;
    left: -0.2vw;
    border-width: 0.4vw 0 0 0.4vw;
  }

  .dojo-container::after {
    bottom: -0.2vw;
    right: -0.2vw;
    border-width: 0 0.4vw 0.4vw 0;
  }

  .header-title {
    flex: 1;
    text-align: center;
  }

  /* 頂部橫幅*/
  .dojo-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 1.5vh 2.5vw;
    background: linear-gradient(90deg, rgba(255, 0, 85, 0.25) 0%, #05050a 50%, rgba(0, 255, 204, 0.2) 100%);
    border-bottom: 0.3vw solid;
    border-image: linear-gradient(90deg, #ff0055, #9d4edd, #00ffcc) 1;
    flex-shrink: 0;
  }

  .header-kamon {
    font-size: 2.2vw;
    font-weight: 900;
    color: #ff0055;
    filter: drop-shadow(0 0 1vw #ff0055);
    animation: kamon-glow 1.5s ease-in-out infinite;
  }

  .header-kamon--right {
    color: #00ffcc;
    filter: drop-shadow(0 0 1vw #00ffcc);
    animation-delay: 0.75s;
  }

  @keyframes kamon-glow {
    0%, 100% {
      transform: scale(1);
      filter: drop-shadow(0 0 0.5vw currentColor);
    }

    50% {
      transform: scale(1.15);
      filter: drop-shadow(0 0 1.5vw currentColor);
    }
  }

  .title-jp {
    display: block;
    font-size: 2.6vw; 
    font-weight: 900;
    letter-spacing: 0.35em;
    background: linear-gradient(45deg, #ffffff 10%, #ff0055 55%, #ffb703 100%);
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
    filter: drop-shadow(0 0 1.5vw rgba(255, 0, 85, 0.9));
  }

  .title-sub {
    display: block;
    font-family: var(--font-mono);
    font-size: 0.9vw;
    font-weight: bold;
    letter-spacing: 0.4em;
    color: #00ffcc;
    margin-top: 0.4vh;
    text-shadow: 0 0 0.6vw rgba(0, 255, 204, 0.8);
  }

  .neon-divider {
    display: flex;
    align-items: center;
    flex-shrink: 0;
    height: 0.5vh;
    background: linear-gradient(90deg, transparent, #ff0055, #ffb703, #00ffcc, transparent);
    position: relative;
  }

  .divider-diamond {
    position: absolute;
    left: 50%;
    top: 50%;
    transform: translate(-50%, -50%) rotate(45deg);
    color: #ffb703;
    font-size: 1.2vw;
    text-shadow: 0 0 1vw #ffb703;
    background: #040408;
    padding: 0.2vw;
  }

  /* 登入畫面彈性 */
  .login-scroll {
    flex: 1;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 2vw;
  }

  .scroll-content {
    width: 32vw;
    min-width: 280px;
    background: rgba(3, 3, 6, 0.95);
    border: 0.15vw solid #ff0055;
    border-radius: 0.4vw;
    padding: 3vh 2.5vw;
    box-shadow: 0 0 4vw rgba(255, 0, 85, 0.3);
  }

  .scroll-label {
    font-size: 2vw;
    font-weight: 900;
    letter-spacing: 0.4em;
    text-align: center;
    margin-bottom: 3.5vh;
    background: linear-gradient(90deg, #ff0055, #ffb703, #00ffcc);
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
    filter: drop-shadow(0 0 1vw rgba(255, 0, 85, 0.8));
  }

  .field-group {
    margin-bottom: 2.5vh;
  }

  .field-label {
    display: block;
    font-family: var(--font-mono);
    font-size: 1.1vw;
    font-weight: 900;
    color: #00ffcc;
    margin-bottom: 0.8vh;
    text-shadow: 0 0 0.5vw rgba(0, 255, 204, 0.6);
  }

  .input-wrap {
    display: flex;
    align-items: center;
    border: 0.15vw solid rgba(255, 0, 85, 0.6);
    background: #000;
  }

    .input-wrap:focus-within {
      border-color: #00ffcc;
      box-shadow: 0 0 1.5vw rgba(0, 255, 204, 0.5);
    }

  .input-glyph {
    width: 3.5vw;
    text-align: center;
    font-size: 1.5vw;
    font-weight: 900;
    color: #ff0055;
    border-right: 0.15vw solid rgba(255, 0, 85, 0.4);
    padding: 1vh 0;
    text-shadow: 0 0 0.5vw #ff0055;
  }

  .cyber-input {
    flex: 1;
    background: transparent;
    border: none;
    outline: none;
    color: #ffffff;
    font-family: var(--font-jp);
    font-size: 1.3vw;
    font-weight: 700;
    padding: 1vh 1vw;
    width: 100%;
  }

  .enter-btn {
    width: 100%;
    padding: 1.5vh;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 1vw;
    background: linear-gradient(135deg, #ff0055 0%, #9d4edd 50%, #00ffcc 100%);
    background-size: 200% auto;
    border: none;
    color: #fff;
    font-family: var(--font-jp);
    font-size: 1.4vw;
    font-weight: 900;
    cursor: pointer;
    animation: buttonGlowBreathe 2s infinite alternate;
  }

  @keyframes buttonGlowBreathe {
    0% {
      box-shadow: 0 0 1vw rgba(255, 0, 85, 0.4);
      filter: brightness(1);
    }

    100% {
      box-shadow: 0 0 2vw rgba(0, 255, 204, 0.8);
      filter: brightness(1.2);
    }
  }

  .btn-kanji {
    font-size: 1.8vw;
    font-weight: 900;
    color: #ffb703;
    text-shadow: 0 0 0.6vw #ffb703;
  }

  .btn-text {
    letter-spacing: 0.3em;
  }

  /* ── 聊天主競技場佈局（無縫 vw 比例分欄） ───────────────────────── */
  .chat-arena {
    flex: 1;
    display: flex;
    overflow: hidden;
  }

  /* 側邊欄 */
  .dojo-sidebar {
    width: 14vw;
    flex-shrink: 0;
    border-right: 0.15vw solid rgba(255, 0, 85, 0.4);
    background: #040408;
    display: flex;
    flex-direction: column;
    align-items: center;
    padding: 4vh 1vw;
    gap: 2vh;
  }

  .sidebar-avatar {
    position: relative;
    width: 6vw;
    height: 6vw;
  }

  .avatar-ring {
    width: 6vw;
    height: 6vw;
    border-radius: 50%;
    border: 0.25vw solid #ff0055;
    display: flex;
    align-items: center;
    justify-content: center;
    background: #000;
    box-shadow: 0 0 1.5vw #ff0055;
  }

  .avatar-char {
    font-family: var(--font-mono);
    font-size: 2.5vw;
    font-weight: 900;
    color: #ffffff;
    text-shadow: 0 0 1vw #ff0055;
  }

  .avatar-glow {
    position: absolute;
    inset: -0.5vw;
    border-radius: 50%;
    background: radial-gradient(circle, rgba(255, 0, 85, 0.5) 0%, transparent 70%);
  }

  .sidebar-name {
    font-size: 1.2vw;
    font-weight: 900;
    color: #ffffff;
    text-shadow: 0 0 0.6vw rgba(255, 255, 255, 0.6);
    text-align: center;
    word-break: break-all;
  }

  .sidebar-room {
    font-family: var(--font-mono);
    font-size: 1vw;
    font-weight: 900;
    color: #00ffcc;
    background: rgba(0, 255, 204, 0.15);
    padding: 0.5vh 1vw;
    border-radius: 0.3vw;
    border: 0.1vw solid #00ffcc;
  }

  .sidebar-divider {
    width: 80%;
    height: 0.1vh;
    background: linear-gradient(90deg, transparent, #ff0055, transparent);
  }

  .sidebar-status {
    display: flex;
    align-items: center;
    gap: 0.5vw;
  }

  .status-dot {
    width: 0.6vw;
    height: 0.6vw;
    background: #00ffcc;
    box-shadow: 0 0 0.8vw #00ffcc;
  }

  .status-text {
    font-size: 0.9vw;
    font-weight: 900;
    color: #00ffcc;
  }

  /* ── 聊天訊息區（解決大字跑版核心） ────────────────────────────── */
  .message-dojo {
    flex: 1;
    display: flex;
    flex-direction: column;
    overflow: hidden;
  }

  .messages-scroll {
    flex: 1;
    overflow-y: auto;
    padding: 3vh 2vw;
    display: flex;
    flex-direction: column;
    gap: 2.5vh;
  }

    .messages-scroll::-webkit-scrollbar {
      width: 0.4vw;
    }

    .messages-scroll::-webkit-scrollbar-thumb {
      background: linear-gradient(#ff0055, #00ffcc);
    }

  .msg-row {
    display: flex;
    gap: 1vw;
    align-items: flex-start;
  }

  .msg-row--mine {
    flex-direction: row-reverse;
  }

  .msg-avatar {
    width: 3vw;
    height: 3vw;
    font-size: 1.3vw;
    font-weight: 900;
    border: 0.15vw solid #00ffcc;
    box-shadow: 0 0 0.8vw rgba(0, 255, 204, 0.4);
    display: flex;
    align-items: center;
    justify-content: center;
    background: #000;
    border-radius: 0.3vw;
  }

  .msg-avatar--mine {
    border-color: #ff0055;
    box-shadow: 0 0 0.8vw rgba(255, 0, 85, 0.4);
  }

  .msg-bubble-wrap {
    max-width: 75%;
  }

  .bubble-meta {
    display: flex;
    gap: 0.8vw;
    align-items: baseline;
    margin-bottom: 0.4vh;
  }

  .msg-row--mine .bubble-meta {
    flex-direction: row-reverse;
  }

  .bubble-user {
    font-size: 1.1vw;
    font-weight: 900;
    color: #fff;
  }

  .bubble-room {
    font-size: 0.9vw;
    color: rgba(0, 255, 204, 0.7);
  }

  /* 聊天氣泡字體：完美高對比粗體 1.35vw，動態發光 */
  .msg-bubble {
    position: relative;
    padding: 1.2vh 1.4vw;
    font-size: 1.35vw; /* 依據視窗自動縮放的大字體，不爆版 */
    font-weight: 700;
    line-height: 1.6;
    color: #ffffff;
    border: 0.15vw solid rgba(0, 255, 204, 0.5);
    background: linear-gradient(135deg, rgba(12, 22, 44, 0.9) 0%, rgba(5, 5, 10, 0.98) 100%);
    box-shadow: 0 0 1vw rgba(0, 255, 204, 0.2);
    animation: BubbleGlowTeal 3s ease-in-out infinite alternate;
    word-break: break-word;
  }

  @keyframes BubbleGlowTeal {
    0% {
      box-shadow: 0 0 0.6vw rgba(0, 255, 204, 0.1);
      border-color: rgba(0, 255, 204, 0.4);
    }

    100% {
      box-shadow: 0 0 1.5vw rgba(0, 255, 204, 0.4);
      border-color: rgba(0, 255, 204, 0.9);
    }
  }

  .msg-row--mine .msg-bubble {
    border-color: rgba(255, 0, 85, 0.5);
    background: linear-gradient(135deg, rgba(45, 10, 25, 0.9) 0%, rgba(10, 5, 8, 0.98) 100%);
    animation: BubbleGlowCrimson 3s ease-in-out infinite alternate;
  }

  @keyframes BubbleGlowCrimson {
    0% {
      box-shadow: 0 0 0.6vw rgba(255, 0, 85, 0.1);
      border-color: rgba(255, 0, 85, 0.4);
    }

    100% {
      box-shadow: 0 0 1.5vw rgba(255, 0, 85, 0.5);
      border-color: rgba(255, 0, 85, 0.9);
    }
  }

  .bubble-corner {
    width: 0.5vw;
    height: 0.5vw;
  }

  .bubble-corner--tl {
    top: -0.15vw;
    left: -0.15vw;
    border-width: 0.15vw 0 0 0.15vw;
  }

  .bubble-corner--br {
    bottom: -0.15vw;
    right: -0.15vw;
    border-width: 0 0.15vw 0.15vw 0;
  }

  /* 系統通知 */
  .sys-banner {
    margin: 1vh 0;
  }

  .sys-line {
    height: 0.2vh;
  }

  .sys-text {
    font-size: 1.1vw;
    font-weight: 900;
    text-shadow: 0 0 1vw #ffb703;
  }

  /*輸入控制列（等比例縮放） */
  .input-dojo {
    display: flex;
    gap: 1vw;
    padding: 2vh 2vw;
    border-top: 0.2vw solid #ff0055;
    background: #030306;
    box-shadow: 0 -0.5vh 2vw rgba(255, 0, 85, 0.2);
    align-items: center;
    flex-shrink: 0;
  }

  .input-blade {
    flex: 1;
    display: flex;
    align-items: center;
    border: 0.15vw solid rgba(255, 0, 85, 0.5);
    background: #000;
  }

    .input-blade:focus-within {
      border-color: #00ffcc;
      box-shadow: 0 0 1.5vw rgba(0, 255, 204, 0.4);
    }

  .blade-tip {
    font-size: 1.5vw;
    padding: 0 1vw;
    text-shadow: 0 0 0.5vw #ff0055;
    font-weight: 900;
  }

  .input-blade:focus-within .blade-tip {
    color: #00ffcc;
    text-shadow: 0 0 0.5vw #00ffcc;
  }

  .blade-input {
    flex: 1;
    background: transparent;
    border: none;
    outline: none;
    color: #ffffff;
    font-family: var(--font-jp);
    font-size: 1.3vw;
    font-weight: 700;
    padding: 1.5vh 1vw 1.5vh 0;
  }

  /* 送出按鈕 */
  .send-btn {
    width: 3.5vw;
    height: 3.5vw;
    min-width: 40px;
    min-height: 40px;
    border: 0.15vw solid #ff0055;
    background: rgba(255, 0, 85, 0.15);
    color: #ff0055;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    box-shadow: 0 0 1vw rgba(255, 0, 85, 0.3);
  }

    .send-btn:hover {
      background: #ff0055;
      color: #ffffff;
      box-shadow: 0 0 2vw #ff0055;
    }

  .send-kanji {
    font-size: 1.5vw;
    font-weight: 900;
    text-shadow: 0 0 0.4vw currentColor;
  }
</style>
