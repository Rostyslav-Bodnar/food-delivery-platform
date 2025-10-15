# frozen_string_literal: true

class PromoUsage < ApplicationRecord
  belongs_to :promo

  validates :account_id, presence: true
end

