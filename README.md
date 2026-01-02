# Druware.API

The Server API for the Druware Website(s)

## Notes

## Todo 

* refactor entity names to lower case with underscores for a more consistant behaviros
  * public.Tag
  * auth.Role
  * auth.User
  * content.Article
  * content.ArticleTag
  * content.Document
  * content.DocumentTag
  * logs.Access
  * public.Asp*

## Change Log

docker buildx build --platform linux/amd64 t druware.azurecr.io/com.druware.api .
docker build -t druware.azurecr.io/com.druware.api .
docker push druware.azurecr.io/com.druware.api