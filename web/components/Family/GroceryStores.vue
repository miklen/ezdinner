<script setup lang="ts">
import { computed, onScopeDispose, shallowRef, watch } from 'vue'
import PickerShell from '~/components/Plan/PickerShell.vue'
import type { Family } from '~/types'

const props = defineProps<{ family: Family; owner: boolean; account: string | null }>()
const emit = defineEmits<{ saved: [storeIds: string[]] }>()
const { t } = useI18n()
const { families } = useRepositories()
const familiesStore = useFamiliesStore()
const draft = shallowRef<string[]>([])
const saving = shallowRef(false)
const failed = shallowRef(false)
const editing = shallowRef(false)
const persisted = computed(() => props.family.groceryStores ?? [])
const stores = computed(() => props.family.supportedGroceryStores ?? [])
const selectedStores = computed(() => persisted.value.map(id => stores.value.find(store => store.id === id)?.name ?? id))
const groups = computed(() => [
  { title: t('grocery.localStores'), stores: stores.value.filter(store => !store.isBorderTrade) },
  { title: t('grocery.borderStores'), stores: stores.value.filter(store => store.isBorderTrade) },
].filter(group => group.stores.length))
const allUnavailable = computed(() => stores.value.length > 0 && stores.value.every(store => !store.isAvailable))
const changed = computed(() => JSON.stringify([...draft.value].sort()) !== JSON.stringify([...persisted.value].sort()))
let generation = 0
watch([() => props.family.id, () => props.account, () => props.owner, persisted], () => {
  generation++
  draft.value = [...persisted.value]
  saving.value = false
  failed.value = false
  editing.value = false
}, { immediate: true })
onScopeDispose(() => generation++)
function openEditor() {
  draft.value = [...persisted.value]
  failed.value = false
  editing.value = true
}
function closeEditor(open: boolean) {
  if (!saving.value) editing.value = open
}
function toggle(id: string, selected: boolean) {
  draft.value = selected ? [...new Set([...draft.value, id])] : draft.value.filter(store => store !== id)
}
async function save() {
  if (!props.owner || saving.value || !changed.value) return
  const token = ++generation
  const familyId = props.family.id
  saving.value = true
  failed.value = false
  try {
    const selection = await families.saveGroceryStores(familyId, [...draft.value])
    if (token !== generation) return
    familiesStore.updateGroceryStores(familyId, selection)
    draft.value = [...selection]
    emit('saved', selection)
    editing.value = false
  }
  catch { if (token === generation) failed.value = true }
  finally { if (token === generation) saving.value = false }
}
</script>

<template>
  <section class="grocery-stores" :aria-label="t('grocery.storesTitle')">
    <div class="grocery-stores__heading">
      <span class="grocery-stores__icon" aria-hidden="true"><v-icon size="20">mdi-storefront-outline</v-icon></span>
      <div>
        <h3 class="grocery-stores__title">{{ t('grocery.storesTitle') }}</h3>
        <p class="grocery-stores__help">{{ t(owner ? 'grocery.storesSummaryHelp' : 'grocery.storesReadOnly') }}</p>
      </div>
    </div>
    <ul v-if="selectedStores.length" class="grocery-stores__selected" :aria-label="t('grocery.selectedStores')">
      <li v-for="name in selectedStores" :key="name"><v-icon size="14" aria-hidden="true">mdi-check</v-icon>{{ name }}</li>
    </ul>
    <p v-else class="grocery-stores__empty">{{ t('grocery.noStoresSelected') }}</p>
    <v-btn v-if="owner" class="grocery-stores__edit" data-test="edit-stores" variant="outlined" size="small" :disabled="saving" @click="openEditor">
      <v-icon size="16" start>mdi-tune-variant</v-icon>{{ t(persisted.length ? 'grocery.editStores' : 'grocery.chooseStores') }}
    </v-btn>

    <PickerShell :open="editing" :title="t('grocery.chooseStores')" :close-label="t('common.close')" @update:open="closeEditor">
      <template #navigation>
        <p class="grocery-stores__help">{{ t('grocery.storesHelp') }}</p>
      </template>
      <div class="grocery-stores__editor">
        <p v-if="allUnavailable" class="grocery-stores__notice"><v-icon size="16" aria-hidden="true">mdi-information-outline</v-icon>{{ t('grocery.offersPending') }}</p>
        <fieldset v-for="group in groups" :key="group.title" class="grocery-stores__group">
          <legend class="grocery-stores__legend">{{ group.title }}</legend>
          <div class="grocery-stores__choices">
            <label v-for="store in group.stores" :key="store.id" class="grocery-stores__choice" :class="{ 'grocery-stores__choice--selected': draft.includes(store.id) }">
              <input type="checkbox" :checked="draft.includes(store.id)" :disabled="!owner || saving" @change="toggle(store.id, !draft.includes(store.id))">
              <span class="grocery-stores__check" aria-hidden="true"><v-icon v-if="draft.includes(store.id)" size="14">mdi-check</v-icon></span>
              <span class="grocery-stores__label">
                <span class="grocery-stores__name">{{ store.name }}</span>
                <span v-if="store.isFlyerOnly" class="grocery-stores__note">{{ t('grocery.flyerOnly') }}</span>
                <span v-if="!allUnavailable && !store.isAvailable" class="grocery-stores__note">{{ t('grocery.offersUnavailable') }}</span>
              </span>
            </label>
          </div>
        </fieldset>
        <p v-if="!stores.length" class="grocery-stores__help">{{ t('grocery.noStores') }}</p>
      </div>
      <template #footer>
        <p v-if="failed" role="alert" class="grocery-stores__error">{{ t('grocery.saveFailed') }}</p>
        <div class="grocery-stores__footer">
          <span class="grocery-stores__count" aria-live="polite">{{ t('grocery.selectedCount', { count: draft.length }) }}</span>
          <v-btn data-test="cancel-stores" variant="text" size="small" :disabled="saving" @click="closeEditor(false)">{{ t('common.cancel') }}</v-btn>
          <v-btn data-test="save-stores" variant="flat" color="primary" size="small" :loading="saving" :disabled="!owner || !changed || saving" @click="save">{{ t('grocery.saveStores') }}</v-btn>
        </div>
      </template>
    </PickerShell>
  </section>
</template>

<style scoped>
.grocery-stores { padding: var(--space-4); border-top: 1px solid var(--color-border); }
.grocery-stores__heading { display: flex; align-items: flex-start; gap: var(--space-3); }
.grocery-stores__icon { display: grid; place-items: center; width: 36px; height: 36px; flex-shrink: 0; border-radius: var(--radius-md); background: rgba(var(--color-primary-rgb), .1); color: var(--color-primary-dark); }
.grocery-stores__title { margin: 0 0 var(--space-1); font-size: var(--text-sm); font-weight: 600; }
.grocery-stores__help { margin: 0; color: var(--color-text-secondary); font-size: var(--text-sm); line-height: 1.5; }
.grocery-stores__empty { margin: var(--space-3) 0; font-size: var(--text-sm); color: var(--color-text-muted); }
.grocery-stores__edit { color: var(--color-primary-dark); border-color: var(--color-border-medium); border-radius: var(--radius-full); text-transform: none; letter-spacing: 0; }
.grocery-stores__selected { display: flex; flex-wrap: wrap; gap: var(--space-2); margin: var(--space-3) 0; padding: 0; list-style: none; }
.grocery-stores__selected li { display: inline-flex; align-items: center; gap: var(--space-1); padding: var(--space-1) var(--space-2); border-radius: var(--radius-full); background: rgba(var(--color-primary-rgb), .08); color: var(--color-primary-dark); font-size: var(--text-xs); overflow-wrap: anywhere; }
.grocery-stores__editor { padding: 0 var(--space-4) var(--space-2); }
.grocery-stores__notice { display: flex; align-items: flex-start; gap: var(--space-2); margin: 0 0 var(--space-4); padding: var(--space-3); border-radius: var(--radius-sm); background: var(--color-surface-variant); color: var(--color-text-secondary); font-size: var(--text-xs); line-height: 1.5; }
.grocery-stores__notice .v-icon { margin-top: 1px; flex-shrink: 0; }
.grocery-stores__group { border: 0; padding: 0; margin: 0 0 var(--space-4); min-width: 0; }
.grocery-stores__group:last-child { margin-bottom: 0; }
.grocery-stores__legend { margin-bottom: var(--space-2); font-size: var(--text-xs); font-weight: 600; color: var(--color-text-muted); }
.grocery-stores__choices { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: var(--space-2); }
.grocery-stores__choice { position: relative; display: flex; align-items: center; gap: var(--space-2); min-height: 52px; padding: var(--space-2) var(--space-3); border: 1px solid var(--color-border-medium); border-radius: var(--radius-md); cursor: pointer; transition: border-color var(--duration-fast), background var(--duration-fast); }
.grocery-stores__choice:hover { border-color: var(--color-primary); background: rgba(var(--color-primary-rgb), .04); }
.grocery-stores__choice--selected { border-color: var(--color-primary); background: rgba(var(--color-primary-rgb), .08); }
.grocery-stores__choice:has(input:focus-visible) { outline: 2px solid var(--color-primary-dark); outline-offset: 2px; }
.grocery-stores__choice:has(input:disabled) { cursor: default; opacity: .65; }
.grocery-stores__choice input { position: absolute; opacity: 0; width: 1px; height: 1px; }
.grocery-stores__check { display: grid; place-items: center; flex-shrink: 0; width: 18px; height: 18px; border: 1px solid var(--color-border-medium); border-radius: var(--space-1); color: var(--color-text-on-primary); background: var(--color-surface); }
.grocery-stores__choice--selected .grocery-stores__check { background: var(--color-primary-dark); border-color: var(--color-primary-dark); }
.grocery-stores__label { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
.grocery-stores__name { font-size: var(--text-sm); font-weight: 500; overflow-wrap: anywhere; line-height: 1.3; }
.grocery-stores__note { font-size: var(--text-xs); color: var(--color-text-muted); line-height: 1.3; }
.grocery-stores__footer { display: flex; align-items: center; gap: var(--space-2); padding-top: var(--space-2); border-top: 1px solid var(--color-border); }
.grocery-stores__count { margin-right: auto; font-size: var(--text-xs); color: var(--color-text-muted); white-space: nowrap; }
.grocery-stores__footer .v-btn { text-transform: none; letter-spacing: 0; }
.grocery-stores__error { margin: 0 0 var(--space-2); font-size: var(--text-sm); color: var(--color-error); }
@media (max-width: 960px) {
  .grocery-stores__edit, .grocery-stores__footer .v-btn { min-height: 44px; }
}
@media (max-width: 360px) {
  .grocery-stores__choices { grid-template-columns: minmax(0, 1fr); }
  .grocery-stores__choice { padding: var(--space-2); }
  .grocery-stores__footer { flex-wrap: wrap; }
  .grocery-stores__count { flex-basis: 100%; }
}
</style>
