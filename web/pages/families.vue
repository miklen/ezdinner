<template>
  <span>
    <v-row>
      <v-col v-for="family in families" :key="family.id" cols="12" sm="12" md="6" lg="4">
        <v-card rounded="lg">
          <v-card-title>{{ family.name }}</v-card-title>
          <v-card-subtitle>Family members</v-card-subtitle>
          <v-list>
            <v-list-item
              v-for="member in family.familyMembers"
              :key="member.id"
              :title="member.name"
              prepend-icon="mdi-account"
            />
          </v-list>
          <v-card-actions>
            <v-btn variant="text" color="primary" @click="openInviteDialog(family.id)">Invite</v-btn>
            <v-btn variant="text" color="primary" @click="openAddMemberDialog(family.id)">Create</v-btn>
          </v-card-actions>
        </v-card>
      </v-col>

      <v-col cols="12" sm="12" md="6" lg="4">
        <v-card rounded="lg">
          <v-card-title>Create family</v-card-title>
          <v-card-text>
            To begin planning you need to create a family. After you've created your family you can
            then invite other family members to participate in planning or create family members
            which are used to rate dishes.
          </v-card-text>
          <v-card-text>You can participate in more than one family!</v-card-text>
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
        <v-card-title class="text-h5">Invite family member</v-card-title>
        <v-divider />
        <v-card-text style="padding-top: 16px">
          Invite someone to join as a family member. Users must have an account before they can be invited.
        </v-card-text>
        <v-card-text>
          <v-text-field
            v-model="inviteEmail"
            autofocus
            placeholder="Family member email address"
            @keyup.enter="inviteMember"
          />
          <v-alert v-model="notFoundAlert" closable type="warning" border="start" variant="tonal">
            User not found
          </v-alert>
          <v-alert v-model="errorAlert" closable type="error" border="start" variant="tonal">
            An error occurred
          </v-alert>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="inviteDialog = false">Cancel</v-btn>
          <v-btn variant="text" color="primary" @click="inviteMember">Invite</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Create family dialog -->
    <v-dialog v-model="newFamilyDialog" width="500">
      <v-card>
        <v-card-title class="text-h5">New family</v-card-title>
        <v-divider />
        <v-card-text style="padding-top: 16px">Give your family a recognizable name.</v-card-text>
        <v-card-text>
          <v-text-field v-model="newFamilyName" autofocus placeholder="Family name" @keyup.enter="createFamily" />
          <v-alert v-model="errorAlert" closable type="error" border="start" variant="tonal">
            An error occurred
          </v-alert>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="newFamilyDialog = false">Cancel</v-btn>
          <v-btn variant="text" color="primary" @click="createFamily">Create</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Create member without account dialog -->
    <v-dialog v-model="addMemberDialog" width="500">
      <v-card>
        <v-card-title class="text-h5">Create family member</v-card-title>
        <v-divider />
        <v-card-text style="padding-top: 16px">
          Create a family member without an account (e.g. children) to track their dish ratings.
        </v-card-text>
        <v-card-text>
          <v-text-field v-model="memberName" autofocus placeholder="Family member name" @keyup.enter="addMember" />
          <v-alert v-model="errorAlert" closable type="error" border="start" variant="tonal">
            An error occurred
          </v-alert>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="addMemberDialog = false">Cancel</v-btn>
          <v-btn variant="text" color="primary" @click="addMember">Create</v-btn>
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

const families = ref<Family[]>([])
const targetFamilyId = ref('')

const inviteDialog = ref(false)
const inviteEmail = ref('')
const notFoundAlert = ref(false)

const newFamilyDialog = ref(false)
const newFamilyName = ref('')

const addMemberDialog = ref(false)
const memberName = ref('')

const errorAlert = ref(false)

onMounted(async () => {
  families.value = await familyRepo.all()
})

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
</script>
