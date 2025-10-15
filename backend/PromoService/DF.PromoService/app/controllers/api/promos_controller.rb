# frozen_string_literal: true

class Api::PromosController < ApplicationController
  def create
    promo = Promo.create!(promo_params)
    render json: promo, status: :created
  end

  def show
    promo = Promo.find_by(code: params[:code])
    if promo
      render json: promo
    else
      render json: { error: "Promo not found" }, status: :not_found
    end
  end

  def validate
    promo = Promo.find_by(code: params[:code])
    if promo&.available_for?(params[:user_id])
      render json: { valid: true, discount_percent: promo.discount_percent }
    else
      render json: { valid: false }, status: :unprocessable_entity
    end
  end

  def use
    promo = Promo.find_by(code: params[:code])
    if promo && promo.available_for?(params[:user_id])
      promo.promo_usages.create!(user_id: params[:user_id], used_at: Time.current)
      render json: { message: "Promo applied" }
    else
      render json: { error: "Promo invalid or already used" }, status: :unprocessable_entity
    end
  end

  private

  def promo_params
    params.require(:promo).permit(:code, :discount_percent, :valid_from, :valid_until,
                                  :restaurant_id, :category_id, :is_global, :usage_limit)
  end
end

