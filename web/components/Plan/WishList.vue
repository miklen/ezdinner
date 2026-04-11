<script setup lang="ts">
import type { WishlistItem } from '~/types'

const wishlistStore = useWishlistStore()
const familiesStore = useFamiliesStore()
const { $msal } = useNuxtApp()
const { show: showSnackbar } = useSnackbar()
const { t } = useI18n()

const addWishDialogOpen = ref(false)

const currentUserId = computed(() => $msal.getObjectId() ?? '')
const familyMembers = computed(() => familiesStore.activeFamily?.familyMembers ?? [])

const isOwner = computed(() =>
  familyMembers.value.some((m) => m.isOwner && m.id === currentUserId.value),
)

const upvotingId = ref<string | null>(null)
const removingId = ref<string | null>(null)

async function upvote(wish: WishlistItem) {
  if (upvotingId.value) return
  upvotingId.value = wish.wishId
  try {
    const { alreadyVoted } = await wishlistStore.upvoteWish(wish.wishId)
    if (alreadyVoted) {
      showSnackbar(t('wishlist.alreadyVoted'), { type: 'info' })
    }
  } catch {
    showSnackbar(t('wishlist.errorUpvoting'), { type: 'error' })
  } finally {
    upvotingId.value = null
  }
}

async function removeWish(wish: WishlistItem) {
  if (removingId.value) return
  removingId.value = wish.wishId
  try {
    await wishlistStore.removeWish(wish.wishId)
  } catch {
    showSnackbar(t('wishlist.errorRemoving'), { type: 'error' })
  } finally {
    removingId.value = null
  }
}

function canRemove(wish: WishlistItem): boolean {
  return wish.addedById === currentUserId.value || isOwner.value
}
</script>

<template>
  <div class="wish-list">
    <!-- Header -->
    <div class="wish-list__header">
      <span class="text-section-title wish-list__title">{{ $t('wishlist.title') }}</span>
      <button class="wish-list__add-btn" @click="addWishDialogOpen = true">
        <v-icon size="16" icon="mdi-plus" />
        {{ $t('wishlist.add') }}
      </button>
    </div>

    <!-- Empty state -->
    <div v-if="wishlistStore.wishes.length === 0" class="wish-list__empty">
      <v-icon size="28" icon="mdi-star-outline" class="wish-list__empty-icon" />
      <p class="wish-list__empty-text">{{ $t('wishlist.emptyState') }}</p>
    </div>

    <!-- Wish rows -->
    <div v-else class="wish-list__items">
      <div
        v-for="wish in wishlistStore.wishes"
        :key="wish.wishId"
        class="wish-row"
      >
        <!-- Left: dish name + requester -->
        <div class="wish-row__info">
          <span class="wish-row__dish">{{ wish.dishName }}</span>
          <span class="wish-row__by">{{ $t('wishlist.addedBy', { name: wish.addedByName || $t('wishlist.someone') }) }}</span>
        </div>

        <!-- Right: vote count + upvote + remove -->
        <div class="wish-row__actions">
          <!-- Vote count badge -->
          <span class="wish-row__votes">
            <v-icon size="13" :icon="wish.isVotedByCurrentUser ? 'mdi-star' : 'mdi-star-outline'" class="wish-row__star" />
            {{ wish.voteCount }}
          </span>

          <!-- +1 button -->
          <v-tooltip theme="dark">
            <template #activator="{ props: tooltipProps }">
              <button
                v-bind="tooltipProps"
                class="wish-row__upvote"
                :class="{ 'wish-row__upvote--voted': wish.isVotedByCurrentUser }"
                :disabled="wish.isVotedByCurrentUser || upvotingId === wish.wishId"
                :aria-label="wish.isVotedByCurrentUser ? $t('wishlist.alreadyVotedTooltip') : $t('wishlist.upvoteTooltip')"
                @click="upvote(wish)"
              >
                <v-progress-circular
                  v-if="upvotingId === wish.wishId"
                  size="12"
                  width="2"
                  indeterminate
                />
                <v-icon v-else size="15" icon="mdi-thumb-up-outline" />
              </button>
            </template>
            {{ wish.isVotedByCurrentUser ? $t('wishlist.alreadyVotedTooltip') : $t('wishlist.upvoteTooltip') }}
          </v-tooltip>

          <!-- Remove button -->
          <v-tooltip v-if="canRemove(wish)" theme="dark">
            <template #activator="{ props: tooltipProps }">
              <button
                v-bind="tooltipProps"
                class="wish-row__remove"
                :disabled="removingId === wish.wishId"
                :aria-label="$t('wishlist.removeTooltip')"
                @click="removeWish(wish)"
              >
                <v-progress-circular
                  v-if="removingId === wish.wishId"
                  size="12"
                  width="2"
                  indeterminate
                />
                <v-icon v-else size="15" icon="mdi-close" />
              </button>
            </template>
            {{ $t('wishlist.removeTooltip') }}
          </v-tooltip>
        </div>
      </div>
    </div>

    <!-- Add wish dialog -->
    <WishAddWishDialog v-model="addWishDialogOpen" />
  </div>
</template>

<style scoped>
.wish-list {
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
}

.wish-list__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-3);
}

.wish-list__title {
  color: var(--color-text-primary);
}

.wish-list__add-btn {
  display: inline-flex;
  align-items: center;
  gap: var(--space-1);
  padding: 4px var(--space-3);
  background: rgba(var(--color-primary-rgb), 0.07);
  border: 1px solid rgba(var(--color-primary-rgb), 0.2);
  border-radius: var(--radius-full);
  font-family: var(--font-body);
  font-size: var(--text-xs);
  font-weight: 600;
  color: var(--color-primary);
  cursor: pointer;
  transition:
    background-color var(--duration-fast) var(--ease-out),
    border-color var(--duration-fast) var(--ease-out);
}

.wish-list__add-btn:hover {
  background: rgba(var(--color-primary-rgb), 0.14);
  border-color: rgba(var(--color-primary-rgb), 0.4);
}

/* Empty state */
.wish-list__empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-2);
  padding: var(--space-6) var(--space-4);
  text-align: center;
}

.wish-list__empty-icon {
  color: var(--color-text-muted);
  opacity: 0.4;
}

.wish-list__empty-text {
  font-size: var(--text-sm);
  color: var(--color-text-muted);
  margin: 0;
}

/* Wish item rows */
.wish-list__items {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.wish-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-3);
  padding: var(--space-2) var(--space-3);
  background: var(--color-surface-variant);
  border-radius: var(--radius-md);
  border: 1px solid var(--color-border);
}

.wish-row__info {
  display: flex;
  flex-direction: column;
  gap: 2px;
  flex: 1;
  min-width: 0;
}

.wish-row__dish {
  font-family: var(--font-body);
  font-size: var(--text-sm);
  font-weight: 600;
  color: var(--color-text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.wish-row__by {
  font-size: var(--text-xs);
  color: var(--color-text-muted);
}

.wish-row__actions {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  flex-shrink: 0;
}

.wish-row__votes {
  display: flex;
  align-items: center;
  gap: 3px;
  font-size: var(--text-xs);
  font-weight: 600;
  color: var(--color-text-secondary);
  min-width: 28px;
  justify-content: flex-end;
}

.wish-row__star {
  color: var(--color-primary);
}

.wish-row__upvote,
.wish-row__remove {
  display: flex;
  align-items: center;
  justify-content: center;
  min-width: 40px;
  min-height: 40px;
  background: none;
  border: none;
  border-radius: var(--radius-sm);
  cursor: pointer;
  color: var(--color-text-muted);
  transition:
    color var(--duration-fast) var(--ease-out),
    background-color var(--duration-fast) var(--ease-out);
}

.wish-row__upvote:hover:not(:disabled) {
  color: var(--color-primary);
  background: rgba(var(--color-primary-rgb), 0.08);
}

.wish-row__upvote--voted {
  color: var(--color-primary) !important;
  cursor: default;
}

.wish-row__upvote:disabled,
.wish-row__remove:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.wish-row__remove:hover:not(:disabled) {
  color: var(--color-error);
  background: rgba(var(--color-error), 0.06);
}
</style>
