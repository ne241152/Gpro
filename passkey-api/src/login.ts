import {
  generateAuthenticationOptions,
  verifyAuthenticationResponse,
} from '@simplewebauthn/server';

const RP_ID = 'ne241152.github.io';
const RP_ORIGIN = 'https://ne241152.github.io';

type AuthResponse =
  Parameters<typeof verifyAuthenticationResponse>[0]['response'];

export async function handleLogin(
  request: Request,
  env: Env,
  headers: HeadersInit
): Promise<Response> {
  const url = new URL(request.url);

  const json = (data: unknown, status = 200) =>
    Response.json(data, { status, headers });

  // ログイン用チャレンジの発行
  if (
    url.pathname === '/login/options' &&
    request.method === 'POST'
  ) {
    try {
      const loginId = crypto.randomUUID();

      const options = await generateAuthenticationOptions({
        rpID: RP_ID,
        userVerification: 'required',
      });

      await env.DB.prepare(
        `INSERT INTO challenges
         (id, user_id, challenge, purpose, expires_at)
         VALUES (?, ?, ?, 'authentication', ?)`
      ).bind(
        loginId,
        'anonymous',
        options.challenge,
        Math.floor(Date.now() / 1000) + 300
      ).run();

      return json({ loginId, options });
    } catch (error) {
      console.error(error);
      return json({ error: 'ログイン準備に失敗しました' }, 500);
    }
  }

  // パスキーの署名を検証
  if (
    url.pathname === '/login/verify' &&
    request.method === 'POST'
  ) {
    let body: {
      loginId?: string;
      response?: AuthResponse;
    };

    try {
      body = await request.json();

      if (
        typeof body.loginId !== 'string' ||
        typeof body.response?.id !== 'string'
      ) {
        return json({ error: 'ログイン情報が不足しています' }, 400);
      }

      // 一度だけ使用できるチャレンジ
      const challenge = await env.DB.prepare(
        `DELETE FROM challenges
         WHERE id = ?
           AND purpose = 'authentication'
           AND expires_at > ?
         RETURNING challenge`
      ).bind(
        body.loginId,
        Math.floor(Date.now() / 1000)
      ).first<{ challenge: string }>();

      if (!challenge) {
        return json({
          error: 'チャレンジが期限切れ、または使用済みです'
        }, 400);
      }

      // 登録済み公開鍵を取得
      const passkey = await env.DB.prepare(
        `SELECT public_key, counter
         FROM passkeys WHERE id = ?`
      ).bind(body.response.id).first<{
        public_key: number[];
        counter: number;
      }>();

      if (!passkey) {
        return json({
          error: '登録済みパスキーが見つかりません'
        }, 400);
      }

      const verification = await verifyAuthenticationResponse({
        response: body.response,
        expectedChallenge: challenge.challenge,
        expectedOrigin: RP_ORIGIN,
        expectedRPID: RP_ID,
        credential: {
          id: body.response.id,
          publicKey: new Uint8Array(passkey.public_key),
          counter: passkey.counter,
        },
        requireUserVerification: true,
      });

      if (!verification.verified) {
        return json({ verified: false }, 400);
      }

      // 認証カウンターを更新
      await env.DB.prepare(
        `UPDATE passkeys SET counter = ? WHERE id = ?`
      ).bind(
        verification.authenticationInfo.newCounter,
        body.response.id
      ).run();

      return json({ verified: true });
    } catch (error) {
      console.error('Login failed:', error);
      return json({ error: 'パスキー認証に失敗しました' }, 400);
    }
  }

  return json({ error: 'Not Found' }, 404);
}
