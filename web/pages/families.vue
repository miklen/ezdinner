<template>
  <span>
    <v-row>
      <v-col v-for="family in families" :key="family.id" cols="12" sm="12" md="6" lg="4">
        <v-card rounded="lg">
          <v-card-title>{{ family.name }}</v-card-title>
          <v-card-subtitle>{{ $t('families.familyMembers') }}</v-card-subtitle>
          <v-list>
            <v-list-item
              v-for="member in family.familyMembers"
              :key="member.id"
              :title="member.name"
              :prepend-icon="member.isOwner ? 'mdi-shield-account' : member.hasAutonomy ? 'mdi-account' : 'mdi-account-outline'"
            >
              <template #append>
                <v-btn
                  v-if="!member.hasAutonomy"
                  icon="mdi-merge"
                  size="small"
                  variant="text"
                  @click.stop="openMergeDialog(family.id, member.id)"
                />
                <v-btn
                  v-if="isOwnerOf(family.id) && member.hasAutonomy && !member.isOwner"
                  icon="mdi-shield-plus"
                  size="small"
                  variant="text"
                  :title="$t('families.makeOwner')"
                  @click.stop="changeRole(family.id, member.id, true)"
                />
                <v-tooltip
                  v-if="isOwnerOf(family.id) && member.hasAutonomy && member.isOwner"
                  theme="dark"
                >
                  <template #activator="{ props: tooltipProps }">
                    <span v-bind="tooltipProps">
                      <v-btn
                        icon="mdi-shield-off"
                        size="small"
                        variant="text"
                        :disabled="ownerCount(family.id) <= 1"
                        @click.stop="changeRole(family.id, member.id, false)"
                      />
                    </span>
                  </template>
                  {{ ownerCount(family.id) <= 1 ? $t('families.mustHaveOneOwner') : $t('families.removeOwner') }}
                </v-tooltip>
              </template>
            </v-list-item>
          </v-list>
          <v-card-actions>
            <v-btn v-if="isOwnerOf(family.id)" variant="text" color="primary" @click="openInviteDialog(family.id)">{{ $t('common.invite') }}</v-btn>
            <v-btn v-if="isOwnerOf(family.id)" variant="text" color="primary" @click="openAddMemberDialog(family.id)">{{ $t('common.create') }}</v-btn>
          </v-card-actions>
        </v-card>
      </v-col>

      <v-col cols="12" sm="12" md="6" lg="4">
        <v-card rounded="lg">
          <v-card-title>{{ $t('families.createFamily') }}</v-card-title>
          <v-card-text>{{ $t('families.createFamilyBody1') }}</v-card-text>
          <v-card-text>{{ $t('families.createFamilyBody2') }}</v-card-text>
          <v-card-actions>
            <v-btn variant="text" color="primary" @click="newFamilyDialog = true">
              <v-icon>mdi-account-multiple-plus</v-icon>
            </v-btn>
          </v-card-actions>
        </v-card>
      </v-col>
    </v-row>

    <!-- Invite member dialog -->
    <v-dialog v-model="inviteDialog" width="500">
      <v-card>
        <v-card-title class="text-h5">{{ $t('families.inviteFamilyMember') }}</v-card-title>
        <v-divider />
        <v-card-text style="padding-top: 16px">{{ $t('families.inviteText') }}</v-card-text>
        <v-card-text>
          <v-text-field
            v-model="inviteEmail"
            autofocus
            :placeholder="$t('families.familyMemberEmail')"
            @keyup.enter="inviteMember"
          />
          <v-alert v-model="notFoundAlert" closable type="warning" border="start" variant="tonal">
            {{ $t('families.userNotFound') }}
          </v-alert>
          <v-alert v-model="errorAlert" closable type="error" border="start" variant="tonal">
            {{ $t('common.anErrorOccurred') }}
          </v-alert>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="inviteDialog = false">{{ $t('common.cancel') }}</v-btn>
          <v-btn variant="text" color="primary" @click="inviteMember">{{ $t('common.invite') }}</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Create family dialog -->
    <v-dialog v-model="newFamilyDialog" width="500">
      <v-card>
        <v-card-title class="text-h5">{{ $t('families.newFamily') }}</v-card-title>
        <v-divider />
        <v-card-text style="padding-top: 16px">{{ $t('families.newFamilyText') }}</v-card-text>
        <v-card-text>
          <v-text-field v-model="newFamilyName" autofocus :placeholder="$t('families.familyName')" @keyup.enter="createFamily" />
          <v-alert v-model="errorAlert" closable type="error" border="start" variant="tonal">
            {{ $t('common.anErrorOccurred') }}
          </v-alert>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="newFamilyDialog = false">{{ $t('common.cancel') }}</v-btn>
          <v-btn variant="text" color="primary" @click="createFamily">{{ $t('common.create') }}</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Merge non-autonomous member dialog -->
    <v-dialog v-model="mergeDialog" width="500">
      <v-card>
        <v-card-title class="text-h5">{{ $t('families.mergeIntoAccount') }}</v-card-title>
        <v-divider />
        <v-card-text style="padding-top: 16px">{{ $t('families.mergeText') }}</v-card-text>
        <v-card-text>
          <v-select
            v-if="mergeIsOwner"
            v-model="mergeAutonomousId"
            :items="mergeTargetOptions"
            item-title="name"
            item-value="id"
            :label="$t('families.mergeInto')"
          />
          <v-alert v-model="mergeErrorAlert" closable type="error" border="start" variant="tonal">
            {{ $t('common.anErrorOccurred') }}
          </v-alert>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="mergeDialog = false">{{ $t('common.cancel') }}</v-btn>
          <v-btn variant="text" color="primary" :disabled="!mergeAutonomousId" @click="mergeMember">{{ $t('common.merge') }}</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Role change snackbar -->
    <v-snackbar v-model="roleChangeSnackbar" color="success" timeout="3000">
      {{ roleChangeMessage }}
    </v-snackbar>

    <!-- Create member without account dialog -->
    <v-dialog v-model="addMemberDialog" width="500">
      <v-card>
        <v-card-title class="text-h5">{{ $t('families.createFamilyMember') }}</v-card-title>
        <v-divider />
        <v-card-text style="padding-top: 16px">{{ $t('families.createFamilyMemberText') }}</v-card-text>
        <v-card-text>
          <v-text-field v-model="memberName" autofocus :placeholder="$t('families.familyMemberName')" @keyup.enter="addMember" />
          <v-alert v-model="errorAlert" closable type="error" border="start" variant="tonal">
            {{ $t('common.anErrorOccurred') }}
          </v-alert>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="addMemberDialog = false">{{ $t('common.cancel') }}</v-btn>
          <v-btn variant="text" color="primary" @click="addMember">{{ $t('common.create') }}</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </span>
</template>

<script setup lang="ts">
import type { Family } from '~/types'

useHead({ title: 'Families' })

const familiesStore = useFamiliesStore()
const { families: familyRepo } = useRepositories()
const { $msal } = useNuxtApp()
const { t } = useI18n()

const userId = computed(() => $msal.getObjectId())

const families = ref<Family[]>([])
const targetFamilyId = ref('')

const inviteDialog = ref(false)
const inviteEmail = ref('')
const notFoundAlert = ref(false)

const newFamilyDialog = ref(false)
const newFamilyName = ref('')

const addMemberDialog = ref(false)
const memberName = ref('')

const mergeDialog = ref(false)
const mergeFamilyId = ref('')
const mergeNonAutonomousId = ref('')
const mergeAutonomousId = ref('')
const mergeErrorAlert = ref(false)

const mergeIsOwner = computed(() => {
  const family = families.value.find(f => f.id === mergeFamilyId.value)
  return family?.familyMembers.some(m => m.isOwner && m.id === userId.value) ?? false
})

const mergeTargetOptions = computed(() => {
  const family = families.value.find(f => f.id === mergeFamilyId.value)
  const members = family?.familyMembers.filter(m => m.hasAutonomy) ?? []
  return members.map(m => ({
    ...m,
    name: m.id === userId.value ? `${m.name} (You)` : m.name,
  }))
})

const errorAlert = ref(false)

const roleChangeSnackbar = ref(false)
const roleChangeMessage = ref('')

onMounted(async () => {
  families.value = await familyRepo.all()
})

function isOwnerOf(familyId: string): boolean {
  const family = families.value.find(f => f.id === familyId)
  return family?.familyMembers.some(m => m.isOwner && m.id === userId.value) ?? false
}

function ownerCount(familyId: string): number {
  const family = families.value.find(f => f.id === familyId)
  return family?.familyMembers.filter(m => m.isOwner).length ?? 0
}

async function changeRole(familyId: string, memberId: string, isOwner: boolean) {
  try {
    await familyRepo.setMemberRole(familyId, memberId, isOwner)
    families.value = await familyRepo.all()
    roleChangeMessage.value = isOwner ? t('families.memberPromoted') : t('families.ownerRoleRemoved')
    roleChangeSnackbar.value = true
  } catch {
    errorAlert.value = true
  }
}

function openInviteDialog(familyId: string) {
  targetFamilyId.value = familyId
  inviteDialog.value = true
}

function openAddMemberDialog(familyId: string) {
  targetFamilyId.value = familyId
  addMemberDialog.value = true
}

async function inviteMember() {
  notFoundAlert.value = false
  errorAlert.value = false
  try {
    const invited = await familyRepo.inviteFamilyMember(targetFamilyId.value, inviteEmail.value)
    if (!invited) { notFoundAlert.value = true; return }
    inviteDialog.value = false
    inviteEmail.value = ''
    families.value = await familyRepo.all()
  } catch {
    errorAlert.value = true
  }
}

async function createFamily() {
  errorAlert.value = false
  try {
    const ok = await familyRepo.createFamily(newFamilyName.value)
    if (ok) {
      familiesStore.getFamilySelectors()
      families.value = await familyRepo.all()
      newFamilyName.value = ''
      newFamilyDialog.value = false
    } else {
      errorAlert.value = true
    }
  } catch {
    errorAlert.value = true
  }
}

async function addMember() {
  await familyRepo.createFamilyMember(targetFamilyId.value, memberName.value)
  addMemberDialog.value = false
  memberName.value = ''
  families.value = await familyRepo.all()
}

function openMergeDialog(familyId: string, nonAutonomousMemberId: string) {
  mergeFamilyId.value = familyId
  mergeNonAutonomousId.value = nonAutonomousMemberId
  mergeErrorAlert.value = false
  // Pre-select the current user if they are an autonomous member of this family
  const family = families.value.find(f => f.id === familyId)
  const selfMember = family?.familyMembers.find(m => m.id === userId.value && m.hasAutonomy)
  mergeAutonomousId.value = selfMember?.id ?? ''
  mergeDialog.value = true
}

async function mergeMember() {
  mergeErrorAlert.value = false
  try {
    const ok = await familyRepo.mergeNonAutonomousMember(mergeFamilyId.value, mergeNonAutonomousId.value, mergeAutonomousId.value)
    if (ok) {
      mergeDialog.value = false
      families.value = await familyRepo.all()
      await familiesStore.getActiveFamily()
    } else {
      mergeErrorAlert.value = true
    }
  } catch {
    mergeErrorAlert.value = true
  }
}
</script>
