const { initializeApp, cert } = require('firebase-admin/app');
const { getAuth } = require('firebase-admin/auth');
const serviceAccount = require('./backend/MaquiLease.API/maquilease-firebase-adminsdk-fbsvc-c134770f08.json');

initializeApp({
  credential: cert(serviceAccount)
});

const createOrUpdateUser = async (email, password, displayName) => {
  try {
    const userRecord = await getAuth().createUser({
      email,
      emailVerified: true,
      password,
      displayName,
      disabled: false,
    });
    console.log(`Successfully created new user: ${email} (${userRecord.uid})`);
  } catch (error) {
    if (error.code === 'auth/email-already-exists') {
      console.log(`User ${email} already exists, attempting to update password...`);
      const user = await getAuth().getUserByEmail(email);
      await getAuth().updateUser(user.uid, { password });
      console.log(`Successfully updated password for ${email}`);
    } else {
      console.error(`Error creating user ${email}:`, error);
    }
  }
};

const run = async () => {
  const users = [
    {
      email: 'operador@maquilease.com',
      password: process.env.OPERADOR_PASSWORD,
      displayName: 'María López',
    },
    {
      email: 'gerente@maquilease.com',
      password: process.env.GERENTE_PASSWORD,
      displayName: 'Luis Vargas',
    },
  ];

  for (const user of users) {
    if (!user.password || user.password.length < 12) {
      throw new Error(`Define una contraseña segura en la variable de entorno para ${user.email}`);
    }

    await createOrUpdateUser(user.email, user.password, user.displayName);
  }

  process.exit(0);
};

run();
