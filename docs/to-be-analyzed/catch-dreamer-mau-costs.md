# Catch Dreamer --- MAU Cost Model

> Planning estimates based on the assumptions discussed in this chat.
> AWS and AI-provider pricing changes; validate live prices before
> launch and periodically thereafter.

## Assumptions

-   95% Free / 5% paying users.
-   Free: 3 interpretations/month and 0.1 images/month on average.
-   Paying: 15 interpretations/month and 5 images/month.
-   Average interpretation: 1,000 input + 700 output tokens.
-   Planning text cost used in our discussion:
    **\~\$0.00104/interpretation**.
-   Low-cost image planning assumption: **\~\$0.01/image**.
-   Embeddings: negligible relative to image generation in this model.

## Expected Usage

          MAU   Interpretations/month   Images/month
  ----------- ----------------------- --------------
        1,000                 \~3,600          \~345
       10,000                \~36,000        \~3,450
      100,000               \~360,000       \~34,500
    1,000,000             \~3,600,000      \~345,000

## Estimated AI Cost

          MAU    AI/month
  ----------- -----------
        1,000       \~\$7
       10,000      \~\$73
      100,000     \~\$727
    1,000,000   \~\$7,266

Image generation is expected to dominate AI cost, so images should have
plan limits/credits and normally be generated only on request.

## Estimated AWS Infrastructure Cost --- Excluding AI

          MAU        AWS/month
  ----------- ----------------
        1,000       \~\$20--35
       10,000       \~\$30--60
      100,000     \~\$100--300
    1,000,000   \~\$700--2,000

Actual costs depend on RDS sizing, backups, logging, traffic, data
transfer, image storage, observability and workload shape.

## Combined Planning Estimate

          MAU          AI        AWS infra            Total/month
  ----------- ----------- ---------------- ----------------------
        1,000       \~\$7       \~\$20--35         **\~\$27--42**
       10,000      \~\$73       \~\$30--60       **\~\$103--133**
      100,000     \~\$727     \~\$100--300     **\~\$827--1,027**
    1,000,000   \~\$7,266   \~\$700--2,000   **\~\$7,966--9,266**

Excluded: app-store commissions, VAT/taxes, marketing/CAC, refunds,
support, development and third-party SaaS.

## Illustrative Revenue

At 1M MAU and 5% paid conversion: - 50,000 paying users. - At \~€3/month
blended subscription revenue: **\~€150,000/month gross**.

At 2% paid conversion: - 20,000 paying users. - At \~€3/month:
**\~€60,000/month gross**.

The larger business risks are likely **CAC, retention and paid
conversion**, not raw text-token cost.

## Cost Optimization

1.  Cache/persist every interpretation.
2.  Store generated images in S3 and reuse them.
3.  Do not automatically generate an image for every dream.
4.  Use cheap models for metadata/background tasks.
5.  Use stronger models only for Deep Interpretation.
6.  Use pgvector retrieval instead of sending complete journals.
7.  Track tokens/cost per request and per user.
8.  Apply monthly limits/credits to expensive features.
9.  Control CloudWatch retention.
10. Recalculate unit economics from real production data.
