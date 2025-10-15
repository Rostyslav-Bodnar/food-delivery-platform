Rails.application.routes.draw do
  namespace :api do
    resources :promos, only: [:create, :show]
    post "promos/validate", to: "promos#validate"
    patch "promos/use", to: "promos#use"
    post "promos/assign-on-register", to: "promos#assign_on_register"
  end
end
