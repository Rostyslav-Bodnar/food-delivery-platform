# frozen_string_literal: true

class CreatePromoUsages < ActiveRecord::Migration[7.0]
  def change
    create_table :promo_usages do |t|
      t.references :promo, null: false, foreign_key: true
      t.integer :user_id, null: false
      t.datetime :used_at

      t.timestamps
    end

    add_index :promo_usages, [:promo_id, :user_id], unique: true
  end
end
