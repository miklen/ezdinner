<template>
  <NuxtLayout>
    <NuxtPage :keepalive="{ include: 'WeekPlanningPage', max: 1 }" :transition="{ name: 'page', mode: 'out-in' }" />
  </NuxtLayout>
</template>
