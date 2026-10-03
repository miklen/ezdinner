import { recommendationRequestSchema, recommendationResultSchema, undoRequestSchema, undoResultSchema, menuChangeRequestSchema, menuChangeResultSchema } from '~/types/dish-recommendations'
import type { RecommendationRequest, UndoRequest, MenuChangeRequest } from '~/types/dish-recommendations'

type ApiFetch = <T>(path: string, options?: Parameters<typeof $fetch>[1]) => Promise<T>

export class DishRecommendationsRepository {
  constructor(private apiFetch: ApiFetch) {}

  async recommend(familyId: string, request: RecommendationRequest, signal?: AbortSignal) {
    const response = await this.apiFetch<unknown>(`/api/families/${familyId}/dish-recommendations`, {
      method: 'POST', body: recommendationRequestSchema.parse(request), signal,
    })
    return recommendationResultSchema.parse(response)
  }

  async undo(familyId: string, date: string, request: UndoRequest) {
    const response = await this.apiFetch<unknown>(`/api/families/${familyId}/dinners/${date}/undo-menu-change`, {
      method: 'POST', body: undoRequestSchema.parse(request), ignoreResponseError: true,
    })
    return undoResultSchema.parse(response)
  }

  async changeMenu(familyId: string, date: string, kind: 'added' | 'removed', request: MenuChangeRequest) {
    const response = await this.apiFetch<unknown>(kind === 'added' ? '/api/dinners/menuitem' : '/api/dinners/menuitem/remove', {
      method: 'PUT', headers: { 'X-EzDinner-Mutation': 'conditional' },
      body: { familyId, date, ...menuChangeRequestSchema.parse(request) }, ignoreResponseError: true,
    })
    return menuChangeResultSchema.parse(response)
  }
}
