import {
  generateRegistrationOptions,
  verifyRegistrationResponse,
} from '@simplewebauthn/server';

const RP_ID = 'ne241152.github.io';
const RP_ORIGIN = 'https://ne241152.github.io';

type RegistrationResponse =
  Parameters<typeof verifyRegistrationResponse>[0]['response'];

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    const url = new URL(request.url);
    const origin = request.headers.get('Origin');

    const allowedOrigins = [
      RP_ORIGIN,
      'http://localhost:8787',
    ];

    if (origin && !allowedOrigins.includes(origin)) {
      return new Response('Forbidden', { status: 403 });
    }

    const headers = {
      'Access-Control-Allow-Origin': origin || RP_ORIGIN,
      'Access-Control-Allow-Methods': 'POST, OPTIONS',
      'Access-Control-Allow-Headers': 'Content-Type',
      'Cache-Control': 'no-store',
    };

    const json = (data: unknown, status = 200) =>
      Response.json(data, { status, headers });

    if (request.method === 'OPTIONS') {
      return new Response(null, { status: 204, headers });
    }

    if (url.pathname === '/' && request.method === 'GET') {
      return new Response('Gpro Passkey API OK', { headers });
    }

    // パスキー登録用の情報を発行
    if (
      url.pathname === '/register/options' &&
      request.method === 'POST'
    ) {
      try {
        const registrationId = crypto.randomUUID();

        const options = await generateRegistrationOptions({
          rpName: 'Gpro',
          rpID: RP_ID,
          userName: `Guest-${registrationId.slice(0, 8)}`,
          userDisplayName: 'Gpro体験者',
          userID: new Uint8Array(new TextEncoder().encode(registrationId)),
          attestationType: 'none',
          authenticatorSelection: {
            residentKey: 'required',
            userVerification: 'required',
          },
        });

        await env.DB.prepare(
          `INSERT INTO challenges
           (id, user_id, challenge, purpose, expires_at)
           VALUES (?, ?, ?, ?, ?)`
        ).bind(
          registrationId,
          registrationId,
          options.challenge,
          'registration',
          Math.floor(Date.now() / 1000) + 300
        ).run();

        return json({ registrationId, options });
      } catch (error) {
        console.error(error);
        return json({ error: '登録準備に失敗しました' }, 500);
      }
    }

    // iPhoneから返された登録結果を検証
    if (
      url.pathname === '/register/verify' &&
      request.method === 'POST'
    ) {
      let body: {
        registrationId?: string;
        response?: RegistrationResponse;
      };

      try {
        body = await request.json();
      } catch {
        return json({ error: 'JSONが不正です' }, 400);
      }

      if (
        typeof body.registrationId !== 'string' ||
        !body.response
      ) {
        return json({ error: '登録情報が不足しています' }, 400);
      }

      const registrationId = body.registrationId;

      const record = await env.DB.prepare(
        `SELECT user_id, challenge
         FROM challenges
         WHERE id = ?
           AND purpose = 'registration'
           AND expires_at > ?`
      ).bind(
        registrationId,
        Math.floor(Date.now() / 1000)
      ).first<{ user_id: string; challenge: string }>();

      if (!record) {
        return json({
          error: '登録情報が存在しないか期限切れです'
        }, 400);
      }

      try {
        const verification = await verifyRegistrationResponse({
          response: body.response,
          expectedChallenge: record.challenge,
          expectedOrigin: RP_ORIGIN,
          expectedRPID: RP_ID,
          requireUserVerification: true,
        });

        if (!verification.verified || !verification.registrationInfo) {
          return json({ verified: false }, 400);
        }

        const {
          credential,
          credentialDeviceType,
          credentialBackedUp,
        } = verification.registrationInfo;

        // チャレンジを一度だけ使用できるようにする
        const consumed = await env.DB.prepare(
          `DELETE FROM challenges
           WHERE id = ?
             AND purpose = 'registration'
             AND expires_at > ?
           RETURNING user_id`
        ).bind(
          registrationId,
          Math.floor(Date.now() / 1000)
        ).first<{ user_id: string }>();

        if (!consumed) {
          return json({ error: '登録情報が使用済みです' }, 400);
        }

        const userId = consumed.user_id;

        // STEP 7で生成したuserIDのBase64URL表現
        const webauthnUserId = btoa(userId)
          .replace(/\+/g, '-')
          .replace(/\//g, '_')
          .replace(/=+$/, '');

        const publicKey = Uint8Array.from(
          credential.publicKey
        ).buffer;

        await env.DB.batch([
          env.DB.prepare(
            `INSERT INTO users (id, webauthn_user_id)
             VALUES (?, ?)`
          ).bind(userId, webauthnUserId),

          env.DB.prepare(
            `INSERT INTO passkeys
             (id, user_id, public_key, counter,
              device_type, backed_up, transports)
             VALUES (?, ?, ?, ?, ?, ?, ?)`
          ).bind(
            credential.id,
            userId,
            publicKey,
            credential.counter,
            credentialDeviceType,
            credentialBackedUp ? 1 : 0,
            JSON.stringify(credential.transports ?? [])
          ),
        ]);

        return json({ verified: true });
      } catch (error) {
        console.error('Registration verification failed:', error);
        return json({
          error: 'パスキーの検証または保存に失敗しました'
        }, 400);
      }
    }

    return new Response('Not Found', {
      status: 404,
      headers,
    });
  },
} satisfies ExportedHandler<Env>;
