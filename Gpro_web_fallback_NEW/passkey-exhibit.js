(() => {
  'use strict';
  const API = 'https://passkey-api.gpro-security.workers.dev';
  const KEY = 'gpro.demoCredentials';
  let stage = 'idle';
  let options = null;
  let fetchedAt = 0;
  let requesting = false;

  const style = document.createElement('style');
  style.textContent = [
    '#gpro-door{position:fixed;inset:0;z-index:100000;display:none;',
    'align-items:center;justify-content:center;box-sizing:border-box;',
    'padding:10px;background:rgba(4,11,24,.87);font-family:system-ui,sans-serif;color:white;}',
    '#gpro-door article{box-sizing:border-box;background:#14243e;border:1px solid #80caff;',
    'width:min(95vw,450px);max-height:calc(100dvh - 20px);overflow:auto;',
    'border-radius:14px;padding:18px;text-align:center;}',
    '#gpro-door h2{font-size:20px;margin:4px 0 12px;}',
    '#gpro-door p{font-size:14px;line-height:1.5;}',
    '#gpro-door label{display:block;text-align:left;margin:10px 0 4px;}',
    '#gpro-door input{width:100%;box-sizing:border-box;padding:12px;',
    'font-size:16px;border:1px solid #adbfd3;border-radius:8px;}',
    '#gpro-door button{display:block;width:100%;padding:11px;',
    'margin:10px 0;min-height:44px;border:0;border-radius:9px;',
    'background:#80caff;color:#102030;font-size:16px;font-weight:600;}',
    '#gpro-door button.alt{background:#38526f;color:white;}',
    '#gpro-door button:disabled{opacity:.5;}',
    '#gpro-door .demo{background:#0c1a2c;padding:10px;',
    'border-radius:8px;overflow-wrap:anywhere;}',
    '#gpro-door a{color:#a9dcff;}',
    '#gpro-door [hidden]{display:none!important;}'
  ].join('');
  document.head.appendChild(style);

  const root = document.createElement('div');
  root.id = 'gpro-door';
  root.setAttribute('role', 'dialog');
  root.setAttribute('aria-modal', 'true');
  root.innerHTML = [
    '<article>',
    '<section id="gpro-fake">',
      '<h2>扉 1：入室確認</h2>',
      '<p>この扉ではIDとパスワードの入力が求められます。</p>',
      '<p class="demo">ゲーム用ID：<strong id="gpro-demo-id"></strong>',
      '<span style="display:block">ゲーム用パスワード：',
      '<strong id="gpro-demo-password"></strong></span></p>',
      '<form id="gpro-fake-form" autocomplete="off">',
      '<label for="gpro-input-id">ID</label>',
      '<input id="gpro-input-id" type="text" autocomplete="off" required>',
      '<label for="gpro-input-pass">パスワード</label>',
      '<input id="gpro-input-pass" type="password" autocomplete="off" required>',
      '<button type="submit">入力して進む</button></form>',
      '<button id="gpro-fake-decline" class="alt">入力しない</button>',
      '<p>普段お使いのID・パスワードは入力しないでください。</p>',
    '</section>',
    '<section id="gpro-result" hidden>',
      '<h2 id="gpro-result-title"></h2>',
      '<p id="gpro-result-text"></p>',
      '<button id="gpro-next-door">次の扉へ進む</button>',
    '</section>',
    '<section id="gpro-real" hidden>',
      '<h2>扉 2：パスキー認証</h2>',
      '<p>この扉はパスキーで認証できます。</p>',
      '<button id="gpro-real-auth" disabled>準備中...</button>',
      '<p id="gpro-real-status" role="status">認証情報を取得中...</p>',
      '<p>パスキー未登録の場合は <a href="./register.html">',
      '登録ページ</a> に戻ってください。</p>',
    '</section>',
    '</article>'
  ].join('');
  document.body.appendChild(root);

  const $ = id => document.getElementById(id);
  const authButton = $('gpro-real-auth');
  const authStatus = $('gpro-real-status');

  function setStage(value) {
    stage = value;
    $('gpro-fake').hidden = value !== 'fake';
    $('gpro-result').hidden = value !== 'result';
    $('gpro-real').hidden = value !== 'real';
    root.style.display = 'flex';
  }

  function credentials() {
    try {
      const value = JSON.parse(sessionStorage.getItem(KEY) || 'null');
      if (value && typeof value.id === 'string' &&
          typeof value.password === 'string') return value;
    } catch (error) { console.warn('Gpro demo credentials:', error); }

    // 直接ゲームを開いた場合も偽ドアの体験用情報を発行する。
    const bytes = crypto.getRandomValues(new Uint8Array(5));
    const hex = Array.from(bytes, v => v.toString(16).padStart(2, '0'))
      .join('').toUpperCase();
    const value = {id: 'GPRO-' + hex.slice(0, 6),
                   password: 'DEMO-' + hex.slice(2, 10)};
    try { sessionStorage.setItem(KEY, JSON.stringify(value)); } catch (error) {}
    return value;
  }

  window.GproShowPasskeyDoor = function () {
    options = null;
    fetchedAt = 0;
    requesting = false;
    const c = credentials();
    $('gpro-demo-id').textContent = c.id;
    $('gpro-demo-password').textContent = c.password;
    setStage('fake');
  };

  function finishFake(entered) {
    // フォームの入力値は送信・記録しない。
    $('gpro-input-id').value = '';
    $('gpro-input-pass').value = '';
    $('gpro-result-title').textContent =
      entered ? 'フィッシングに遭遇！' : '入力を見送りました';
    $('gpro-result-text').textContent = entered
      ? '入力した情報が盗まれる危険がありました。これはゲーム上の演出で、実際には送信・保存していません。'
      : 'ここでは情報を入力せず、次の扉を確認します。';
    setStage('result');
  }

  $('gpro-fake-form').addEventListener('submit', event => {
    event.preventDefault();
    finishFake(true);
  });
  $('gpro-fake-decline').addEventListener('click', () => finishFake(false));
  $('gpro-next-door').addEventListener('click', () => {
    setStage('real');
    prepareLogin();
  });

  window.GproShowPasskeyAuth = function () {
    if (stage !== 'real') return;
    root.style.display = 'flex';
    if (!options || Date.now() - fetchedAt > 240000) prepareLogin();
  };

  async function prepareLogin() {
    if (requesting) return;
    authButton.disabled = true;
    authStatus.textContent = '認証の準備中...';
    try {
      const response = await fetch(API + '/login/options',
        {method: 'POST', cache: 'no-store'});
      if (!response.ok) throw new Error('準備API: HTTP ' + response.status);
      const data = await response.json();
      if (!data.loginId || !data.options) throw new Error('認証情報が不正です');
      options = data;
      fetchedAt = Date.now();
      authButton.textContent = 'パスキーで認証';
      authStatus.textContent = '準備完了。ボタンを押してください。';
    } catch (error) {
      options = null;
      authButton.textContent = '準備を再試行';
      authStatus.textContent = '準備エラー: ' + error.message;
    } finally {
      authButton.disabled = false;
    }
  }

  authButton.addEventListener('click', async () => {
    if (requesting || stage !== 'real') return;
    if (!options || Date.now() - fetchedAt > 240000) {
      await prepareLogin();
      return; // Safariでは次のタップでWebAuthnを開始する
    }
    if (!window.SimpleWebAuthnBrowser) {
      authStatus.textContent = '認証ライブラリが読み込まれていません。再読み込みしてください。';
      return;
    }

    const {loginId, options: optionsJSON} = options;
    options = null;
    requesting = true;
    authButton.disabled = true;
    authStatus.textContent = '端末でパスキー認証してください...';
    try {
      // ユーザーのタップから直接WebAuthnを開始する。
      const operation = window.SimpleWebAuthnBrowser.startAuthentication({
        optionsJSON
      });
      const assertion = await operation;
      authStatus.textContent = 'Cloudflareで検証中...';
      const response = await fetch(API + '/login/verify', {
        method: 'POST',
        headers: {'Content-Type': 'application/json'},
        body: JSON.stringify({loginId, response: assertion})
      });
      const result = await response.json();
      if (!response.ok || result.verified !== true) {
        throw new Error(result.error || '認証に失敗しました');
      }
      if (!window.GproUnityInstance) {
        throw new Error('Unity側との通信が確認できません');
      }
      window.GproUnityInstance.SendMessage('PasskeyAuthBridge', 'OnPasskeySuccess');
      stage = 'complete';
      root.style.display = 'none';
    } catch (error) {
      authStatus.textContent = '認証エラー: ' + error.message;
      console.error('Gpro passkey:', error);
      try {
        window.GproUnityInstance?.SendMessage(
          'PasskeyAuthBridge', 'OnPasskeyFailure', String(error.message)
        );
      } catch (notifyError) { console.error(notifyError); }
    } finally {
      requesting = false;
      if (stage !== 'complete') {
        authButton.disabled = false;
        authButton.textContent = '認証を再試行';
        prepareLogin();
      }
    }
  });
})();
