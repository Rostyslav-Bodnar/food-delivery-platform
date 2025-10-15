# frozen_string_literal: true

class Promo < ApplicationRecord
  has_many :promo_usages

  validates :code, presence: true, uniqueness: true
  validates :discount_percent, presence: true

  scope :active, -> { where('valid_until > ?', Time.current) }

  def available_for?(user_id)
    return false if promo_usages.exists?(user_id: user_id)
    usage_limit.nil? || promo_usages.count < usage_limit
  end
end

