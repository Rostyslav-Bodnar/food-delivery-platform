
import type { AccountResponse } from "../accounts/AccountResponse"
import type { UserDto } from "../user/UserDto"

export interface ProfileResponse {
    user: UserDto
    currentAccount: AccountResponse
    accounts: AccountResponse[]
}
