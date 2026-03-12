<template>
  <v-app>
    <v-app-bar color="white" height="100" elevation="1">
      <v-avatar class="ml-2">
        <v-img src="/android-chrome-192x192.png" />
      </v-avatar>
      <v-toolbar-title class="font-weight-black text-h6">Dinner Planner</v-toolbar-title>
      <v-spacer />
      <v-btn color="primary" size="large" class="mr-4" @click="$msal.signIn()">
        <span class="font-weight-bold">SIGN IN</span>
      </v-btn>
    </v-app-bar>

    <v-main>
      <section id="hero">
        <v-img
          src="/images/pexels-fauxels-3184183-l.jpg"
          min-height="100vh"
          cover
        >
          <v-theme-provider theme="dark">
            <v-container class="fill-height">
              <v-row align="center" justify="center" class="text-white">
                <v-col cols="12" class="text-center" tag="h1">
                  <span :class="smAndDown ? 'text-h4' : 'text-h3'" class="font-weight-light">
                    EASY
                  </span>
                  <br />
                  <span :class="smAndDown ? 'text-h3' : 'text-h2'" class="font-weight-black">
                    DINNER PLANNER
                  </span>
                </v-col>
                <v-btn fab outlined @click="goTo('#about')">
                  <v-icon>mdi-chevron-double-down</v-icon>
                </v-btn>
              </v-row>
            </v-container>
          </v-theme-provider>
        </v-img>
      </section>

      <section id="about">
        <div class="py-12" />
        <v-container class="text-center">
          <h2 class="text-h4 font-weight-bold mb-3">ABOUT</h2>
          <v-responsive class="mx-auto mb-8" width="56">
            <v-divider class="mb-1" />
            <v-divider />
          </v-responsive>
          <v-responsive class="mx-auto text-h6 font-weight-light mb-8" max-width="720">
            Easy Dinner Planner makes it easy to track and plan your family dinner.
            You can track your top dishes, and get suggestions for what to put on the menu the coming week.
          </v-responsive>
          <v-btn color="primary" size="large" @click="$msal.signIn()">
            <span class="font-weight-bold">GET STARTED</span>
          </v-btn>
        </v-container>
        <div class="py-12" />
      </section>

      <section id="features" class="bg-grey-lighten-3">
        <div class="py-12" />
        <v-container class="text-center">
          <h2 class="text-h4 font-weight-bold mb-3">EASY DINNER PLANNER FEATURES</h2>
          <v-responsive class="mx-auto mb-12" width="56">
            <v-divider class="mb-1" />
            <v-divider />
          </v-responsive>
          <v-row>
            <v-col v-for="(feature, i) in features" :key="i" cols="12" md="4">
              <v-card class="py-12 px-4" color="grey-lighten-5" flat>
                <v-theme-provider theme="dark">
                  <v-avatar color="primary" size="88" class="mb-4">
                    <v-icon size="large">{{ feature.icon }}</v-icon>
                  </v-avatar>
                </v-theme-provider>
                <v-card-title class="justify-center font-weight-black text-uppercase">
                  {{ feature.title }}
                </v-card-title>
                <v-card-text class="text-subtitle-1">{{ feature.text }}</v-card-text>
              </v-card>
            </v-col>
          </v-row>
        </v-container>
        <div class="py-12" />
      </section>
    </v-main>

    <v-footer class="justify-center bg-grey-darken-4">
      <div class="text-h6 font-weight-light text-grey-lighten-1 text-center py-4">
        &copy; {{ new Date().getFullYear() }} — EzLifehacks
      </div>
    </v-footer>
  </v-app>
</template>

<script setup lang="ts">
const { $msal } = useNuxtApp()
const { smAndDown } = useDisplay()
const goTo = useGoTo()

const features = [
  { icon: 'mdi-account-group-outline', title: 'Track', text: 'Keep track of what you had for dinner in the past' },
  { icon: 'mdi-update', title: 'Plan', text: 'Plan your meals for the coming week(s)' },
  { icon: 'mdi-shield-outline', title: 'Suggestions', text: 'Out of ideas? Get suggestions based on your previous meals.' },
]

// Redirect to /home if already logged in
onMounted(() => {
  if ($msal.isAuthenticated.value) navigateTo('/home')
})

watch($msal.isAuthenticated, (val) => {
  if (val) navigateTo('/home')
})
</script>
