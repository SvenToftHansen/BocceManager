import Image from "next/image";
import { prisma } from "@/lib/prisma";
import heroImage from "../../public/images/bocce-hero.jpg";

export default async function Home() {
  const leagues = await prisma.leagues.findMany({
    where: { IsActive: true, Seasons: { some: { IsCurrent: true } } },
    include: { Seasons: { where: { IsCurrent: true } } },
    orderBy: { Name: "asc" },
  });

  const currentSeasons = leagues.flatMap((league) =>
    league.Seasons.map((season) => ({ league, season }))
  );

  return (
    <div className="flex flex-1 flex-col">
      <section className="relative flex min-h-[70vh] items-center justify-center overflow-hidden">
        <Image
          src={heroImage}
          alt="Bocce balls on the court"
          fill
          priority
          className="object-cover"
        />
        <div className="absolute inset-0 bg-black/55" />
        <div className="relative z-10 flex flex-col items-center gap-4 px-6 text-center text-white">
          <h1 className="text-4xl font-semibold tracking-tight sm:text-6xl">
            Golden Vista Bocce League
          </h1>
          <p className="max-w-xl text-lg text-zinc-200 sm:text-xl">
            Standings, schedules, and league info for players and fans.
          </p>
        </div>
      </section>

      <section className="mx-auto w-full max-w-4xl flex-1 px-6 py-16">
        <h2 className="mb-8 text-center text-2xl font-semibold tracking-tight text-foreground">
          Current Seasons
        </h2>

        {currentSeasons.length === 0 ? (
          <p className="text-center text-muted-foreground">
            No current seasons are underway right now. Check back soon.
          </p>
        ) : (
          <div
            className={
              currentSeasons.length === 1
                ? "flex justify-center"
                : "grid gap-6 sm:grid-cols-2"
            }
          >
            {currentSeasons.map(({ league, season }) => (
              <div
                key={season.Id}
                className="w-full max-w-sm rounded-2xl border border-border bg-card p-6 text-card-foreground shadow-sm"
              >
                <h3 className="text-xl font-semibold">{league.Name}</h3>
                <p className="mt-1 text-muted-foreground">{season.Name}</p>
                <p className="mt-4 text-sm text-muted-foreground">
                  Standings and schedules coming soon.
                </p>
              </div>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
