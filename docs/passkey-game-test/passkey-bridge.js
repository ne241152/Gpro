(() => {
  const API = 'https://passkey-api.gpro-security.workers.dev';

  // テスト用ボタン
  const testButton = document.createElement('button');
  testButton.textContent = '🔑 パスキー認証テスト';
  Object.assign(testButton.style, {
    position: 'fixed',
    left: '12px',
    bottom: '12px',
    zIndex: '99999',
    padding: '10px',
    fontSize: '14px'
  });
  document.body.appendChild(testButton);

  // 認証用のポップアップ
  const overlay = document.createElement('div');
  Object.assign(overlay.style, {
    display: 'none',
    position: 'fixed',
    inset: '0',
    zIndex: '100000',
    background: 'rgba(0,0,0,0.75)',
    alignItems: 'center',
    justifyContent: 'center'
  });

  overlay.innerHTML = `
    <div style="
      background:#172338;
      color:white;
      padding:28px;
      border-radius:16px;
      text-align:center;
      width:min(85vw,360px);
      font-family:sans-serif;
    ">
      <h2>パスキー認証</h2>
      <button id="gpro-auth-start" disabled>
        準備中...
      </button>
      <p id="gpro-auth-status">ログイン情報を準備中...</p>
      <button id="gpro-auth-close">閉じる</button>
    </div>
  `;

  document.body.appendChild(overlay);

  const authButton = document.getElementById('gpro-auth-start');
  const status = document.getElementById('gpro-auth-status');
  const closeButton = document.getElementById('gpro-auth-close');

  let prepared = null;
  let preparedAt = 0;

  async function prepareLogin() {
    authButton.disabled = true;
    status.textContent = 'ログイン情報を準備中...';

    try {
      const res = await fetch(API + '/login/options', {
        method: 'POST',
        cache: 'no-store'
      });

      if (!res.ok) {
        throw new Error('準備API: HTTP ' + res.status);
      }

      prepared = await res.json();
      preparedAt = Date.now();

      authButton.textContent = 'パスキーで認証';
      status.textContent = '認証準備完了';
    } catch (error) {
      prepared = null;
      authButton.textContent = '準備を再試行';
      status.textContent = '準備エラー: ' + error.message;
    } finally {
      authButton.disabled = false;
    }
  }

  // Unity側から呼び出される関数
  window.GproShowPasskeyAuth = function () {
    overlay.style.display = 'flex';

    if (!prepared || Date.now() - preparedAt > 240000) {
      prepareLogin();
    }
  };

  // ドアUIがない間はテストボタンから起動する
  testButton.addEventListener('click', () => {
    window.GproShowPasskeyAuth();
  });

  closeButton.addEventListener('click', () => {
    overlay.style.display = 'none';
  });

  authButton.addEventListener('click', async () => {
    if (!prepared || Date.now() - preparedAt > 240000) {
      await prepareLogin();
      return;
    }

    const { loginId, options } = prepared;
    prepared = null;
    authButton.disabled = true;

    try {
      status.textContent = 'パスキー認証中...';

      // iPhone Safari対策：クリック直後に開始
      const authPromise =
        SimpleWebAuthnBrowser.startAuthentication({
          optionsJSON: options
        });

      const credential = await authPromise;

      status.textContent = 'Cloudflareで検証中...';

      const res = await fetch(API + '/login/verify', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          loginId,
          response: credential
        })
      });

      const result = await res.json();

      if (!res.ok || !result.verified) {
        throw new Error(result.error || '認証失敗');
      }

      status.textContent = '✅ パスキー認証成功！';
      authButton.textContent = '認証成功';

      // Unityに認証成功を通知
      if (window.GproUnityInstance) {
        try {
          window.GproUnityInstance.SendMessage(
            'PasskeyAuthBridge',
            'OnPasskeySuccess'
          );
          overlay.style.display = 'none';
          console.log('Gpro: Unityに認証成功を通知しました');
        } catch (error) {
          console.error('Unityへの通知エラー:', error);
          status.textContent =
            '✅ 認証成功（Unityへの通知は失敗）';
        }
      } else {
        status.textContent =
          '✅ 認証成功（Unityは未接続）';
      }

    } catch (error) {
      status.textContent =
        '❌ ' + error.name + ': ' + error.message;

      authButton.textContent = '再試行';
      authButton.disabled = false;

      if (window.GproUnityInstance) {
        try {
          window.GproUnityInstance.SendMessage(
            'PasskeyAuthBridge',
            'OnPasskeyFailure',
            String(error.message)
          );
        } catch (notifyError) {
          console.error(notifyError);
        }
      }

      console.error(error);
    }
  });

  // Safari向けに認証情報を事前取得
  prepareLogin();
})();
