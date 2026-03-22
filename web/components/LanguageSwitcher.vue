<script setup lang="ts">
const { locale, setLocale } = useI18n()

const locales = [
  { code: 'en', label: 'English', flag: 'EN' },
  { code: 'da', label: 'Dansk', flag: 'DA' },
]

const currentLabel = computed(() => locale.value.toUpperCase())
</script>

<template>
  <v-menu location="bottom end" :close-on-content-click="true" :offset="6">
    <template #activator="{ props: menuProps }">
      <button
        v-bind="menuProps"
        class="lang-pill"
        :aria-label="locale === 'da' ? 'Sprog' : 'Language'"
      >
        <v-icon size="13" class="lang-pill__icon">mdi-web</v-icon>
        <span class="lang-pill__code">{{ currentLabel }}</span>
      </button>
    </template>

    <div class="lang-menu">
      <button
        v-for="loc in locales"
        :key="loc.code"
        class="lang-menu__item"
        :class="{ 'lang-menu__item--active': locale === loc.code }"
        @click="setLocale(loc.code)"
      >
        <span class="lang-menu__flag">{{ loc.flag }}</span>
        <span class="lang-menu__label">{{ loc.label }}</span>
        <v-icon v-if="locale === loc.code" size="12" class="lang-menu__check">mdi-check</v-icon>
      </button>
    </div>
  </v-menu>
</template>

<style scoped>
/* ── Trigger pill — matches family-pill style ─────────────────────── */
.lang-pill {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 0 var(--space-2);
  height: 32px;
  border-radius: var(--radius-full);
  background-color: var(--color-surface-variant);
  border: 1px solid var(--color-border-medium);
  cursor: pointer;
  transition:
    background-color var(--duration-fast) var(--ease-out),
    border-color var(--duration-fast) var(--ease-out);
  flex-shrink: 0;
}

.lang-pill:hover {
  background-color: var(--color-primary-light);
  border-color: var(--color-primary);
}

.lang-pill__icon {
  color: var(--color-text-muted);
  flex-shrink: 0;
  transition: color var(--duration-fast) var(--ease-out);
}

.lang-pill:hover .lang-pill__icon {
  color: var(--color-surface);
}

.lang-pill__code {
  font-family: var(--font-body);
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.06em;
  color: var(--color-text-secondary);
  line-height: 1;
  transition: color var(--duration-fast) var(--ease-out);
}

.lang-pill:hover .lang-pill__code {
  color: var(--color-surface);
}

/* ── Dropdown card ────────────────────────────────────────────────── */
.lang-menu {
  background: var(--color-surface);
  border-radius: var(--radius-md);
  border: 1px solid var(--color-border-medium);
  box-shadow: var(--shadow-md);
  padding: var(--space-1);
  min-width: 140px;
  overflow: hidden;
}

.lang-menu__item {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  width: 100%;
  padding: var(--space-2) var(--space-3);
  border-radius: var(--radius-sm);
  border: none;
  background: none;
  cursor: pointer;
  transition: background-color var(--duration-fast) var(--ease-out);
  text-align: left;
}

.lang-menu__item:hover {
  background-color: var(--color-surface-variant);
}

.lang-menu__item--active {
  background-color: rgba(var(--color-primary-rgb), 0.07);
}

.lang-menu__item--active:hover {
  background-color: rgba(var(--color-primary-rgb), 0.12);
}

.lang-menu__flag {
  font-family: var(--font-body);
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 0.06em;
  color: var(--color-text-muted);
  background-color: var(--color-surface-variant);
  border: 1px solid var(--color-border-medium);
  border-radius: 4px;
  padding: 1px 5px;
  line-height: 1.5;
  flex-shrink: 0;
}

.lang-menu__item--active .lang-menu__flag {
  color: var(--color-primary-dark);
  background-color: rgba(var(--color-primary-rgb), 0.1);
  border-color: rgba(var(--color-primary-rgb), 0.25);
}

.lang-menu__label {
  flex: 1;
  font-family: var(--font-body);
  font-size: var(--text-sm);
  font-weight: 500;
  color: var(--color-text-secondary);
}

.lang-menu__item--active .lang-menu__label {
  color: var(--color-text-primary);
}

.lang-menu__check {
  color: var(--color-primary);
  flex-shrink: 0;
}
</style>
