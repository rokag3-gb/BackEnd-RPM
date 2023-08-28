docker build -f ".\Dockerfile" --force-rm -t rpmapi:latest .
docker tag rpmapi asia-northeast3-docker.pkg.dev/rpm-2023/rpmapp/rpmapi:latest
docker push asia-northeast3-docker.pkg.dev/rpm-2023/rpmapp/rpmapi:latest
gcloud run deploy rpmapi --quiet --service-account=rpm-api-deploy@rpm-2023.iam.gserviceaccount.com --allow-unauthenticated --image=asia-northeast3-docker.pkg.dev/rpm-2023/rpmapp/rpmapi:latest --platform=managed --region=asia-northeast3 --project=rpm-2023