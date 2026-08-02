# -*- coding: utf-8 -*-
from crewai import LLM
from crewai import Agent, Crew, Task
from crewai import Process

MODEL_CODER = "ollama/qwen3.6:35b"
SERVER = "http://localhost:11434"

MAIN_TASK = """
Solve 1-D heat equation for constant material properties. The left side
of the domain is held at constant temperature, while the opposite end is
exposed to steady air at room temperature and thus submited to convection
and radiative losses.
"""


def main():
    print("Hello from xl-crewai-proto!")

    llm_research = LLM(model=MODEL_CODER, base_url=SERVER)
    llm_coding   = LLM(model=MODEL_CODER, base_url=SERVER)

    researcher = fluid_mechanics_researcher(llm_research)
    coder      = numerical_software_developper(llm_coding)

    research_task = Task(
        description=MAIN_TASK,
        expected_output=(
            "A detailed report with the equations required to model the"
            " problem in LaTeX and the numerical formulation using finite"
            " volume method."
        ),
        agent=researcher,
    )

    coding_task = Task(
        description="Write Python script implementing the model",
        expected_output=(
            "A well written and documented script implementing the required"
            " problem. You are expected to use NumPy for vectorization."
        ),
        agent=coder,
        contexts=[research_task]
    )

    crew = Crew(
        agents  = [researcher, coder],
        tasks   = [research_task, coding_task],
        process = Process.sequential,
        verbose = True,
    )

    result = crew.kickoff()

    print(result.raw)

    from IPython import embed; embed(colors="Linux")



def fluid_mechanics_researcher(llm):
    return Agent(
        role="Senior Fluid Mechanics Researcher",
        goal="Provides the mathematical formulation about a given topic",
        backstory=(
            "You're a seasoned fluid mechanics researcher with a knack "
            " for uncovering the most relevant and accurate information."
            " You're known for your thorough and well-organized research."
        ),
        llm=llm,
    )


def numerical_software_developper(llm):
    return Agent(
        role="Python Numerical Software Developer",
        goal="You develop software for representing a given physical problem",
        backstory=(
            "You're a software developer who masters Python and has a good"
            " grasp of numerical mathematics. You're known for being the"
            " best at translating research papers into functioning apps."
        ),
        llm=llm,
    )


if __name__ == "__main__":
    main()
