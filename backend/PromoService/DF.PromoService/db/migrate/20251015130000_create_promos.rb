# frozen_string_literal: true

class CreatePromos < ActiveRecord::Migration[7.0]
  def change
    create_table :promos do |t|
      t.string :code, null: false
      t.integer :discount_percent, null: false
      t.datetime :valid_from
      t.datetime :valid_until
      t.integer :restaurant_id
      t.integer :category_id
      t.boolean :is_global, default: false
      t.integer :usage_limit

      t.timestamps
    end

    add_index :promos, :code, unique: true
  end
end
