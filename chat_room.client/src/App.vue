<template>
  <div class="cyber-dojo-app">
    <!-- 🪐 核心特效 1：全域數位塌塌米矩陣背景 -->
    <div class="cyber-tatami-grid"></div>

    <!-- 🌊 核心特效 2：全域雷射漸層動態波紋 -->
    <div class="gradient-ripple-overlay"></div>

    <!-- 主要內容渲染區 -->
    <main class="app-content">
      <ChatRoom />
    </main>
  </div>
</template>

<script setup lang="ts">
  import ChatRoom from './components/ChatRoom.vue'; // 💡 請根據你的專案結構調整元件路徑
</script>

<style>
  /* 清除瀏覽器預設邊距 */
  html, body {
    margin: 0;
    padding: 0;
    background-color: #0b0c10;
    overflow-x: hidden;
  }

  /* 定義全域變數 */
  :root {
    --neon-cyan: #00f3ff;
    --neon-pink: #ff0055;
    --neon-gold: #ffb700;
    --tatami-border: #444933;
  }

  .cyber-dojo-app {
    position: relative;
    width: 100%;
    min-height: 100vh;
  }

  /* 🪐 特效 1：數位塌塌米矩陣背景 (改為 fixed 全螢幕固定) */
  .cyber-tatami-grid {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background-image: linear-gradient(to right, var(--tatami-border) 1px, transparent 1px), linear-gradient(to bottom, var(--tatami-border) 1px, transparent 1px);
    background-size: 80px 160px;
    opacity: 0.15;
    z-index: 1;
    pointer-events: none; /* 避免干擾滑鼠點擊 */
  }

  /* 🌊 特效 2：雷射漸層動態波紋 (改為 fixed 全螢幕固定) */
  .gradient-ripple-overlay {
    position: fixed;
    top: -50%;
    left: -50%;
    width: 200%;
    height: 200%;
    background: radial-gradient(circle, transparent 30%, rgba(255, 0, 85, 0.03) 50%, rgba(0, 243, 255, 0.05) 70%, transparent 100%);
    background-size: 100% 100%;
    animation: cyberRipple 12s linear infinite;
    z-index: 1;
    pointer-events: none;
  }

  @keyframes cyberRipple {
    0% {
      transform: rotate(0deg) scale(1);
    }

    50% {
      transform: rotate(180deg) scale(1.2);
    }

    100% {
      transform: rotate(360deg) scale(1);
    }
  }

  /* 確保元件內容浮在特效上方 */
  .app-content {
    position: relative;
    z-index: 2;
  }
</style>
